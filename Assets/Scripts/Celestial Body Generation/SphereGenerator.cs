using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

//A class to generate spheres of a given radius, using different methods
public static class SphereGenerator {
    private static float goldenRatio = (1f + Mathf.Sqrt(5f)) / 2f;

    private static ComputeShader commonOperationsShader = (ComputeShader)Resources.Load("Shaders/Compute Shaders/CommonOperations");
    private static ComputeShader longitudeAndLatitudeShader = (ComputeShader)Resources.Load("Shaders/Compute Shaders/LongitudeAndLatitude");

    private static ComputeBuffer pointBuffer;
    private static ComputeBuffer triangleBuffer;

    //creates a sphere by using longitude and latitude to break up a sphere and get points, then stitch those points together with triangles
    public static Mesh LongitudeAndLatitiudeSphere(float radius, int latitudeLines, int longitudeLines) {
        if (latitudeLines < 1) { throw new ArgumentException("Argument cannot be less than 1", nameof(latitudeLines)); }
        if (longitudeLines < 3) { throw new ArgumentException("Argument cannot be less than 3", nameof(longitudeLines)); }

        //creating all the points that aren't the south or north pole
        SetupPointBufferForLongitudeAndLatitudeSphere(longitudeLines, latitudeLines);
        longitudeAndLatitudeShader.SetFloat("radius", radius);
        longitudeAndLatitudeShader.SetFloat("latitudeAngle", Mathf.PI / (latitudeLines + 1));
        longitudeAndLatitudeShader.SetFloat("longitudeAngle", 2 * Mathf.PI / longitudeLines);
        longitudeAndLatitudeShader.SetInt("longitudeLines", longitudeLines);
        longitudeAndLatitudeShader.SetInt("latitudeLines", latitudeLines);
        int kernel = longitudeAndLatitudeShader.FindKernel("GeneratePoints");
        longitudeAndLatitudeShader.SetBuffer(kernel, "points", pointBuffer);
        longitudeAndLatitudeShader.Dispatch(kernel, Mathf.CeilToInt(latitudeLines / 4f), Mathf.CeilToInt(longitudeLines / 8f), 1);

        //reading the created points into an array
        Vector3[] tempPoints = new Vector3[latitudeLines * longitudeLines];
        pointBuffer.GetData(tempPoints);
        DestroyPointBuffer();
        Vector3[] points = new Vector3[2 + latitudeLines * longitudeLines];
        tempPoints.CopyTo(points, 1);
        points[0] = new Vector3(0, radius, 0);
        points[points.Length - 1] = new Vector3(0, -radius, 0);

        //triangulating the points
        SetupTriangleBufferForLongitudeAndLatitudeSphere(longitudeLines, latitudeLines);
        kernel = longitudeAndLatitudeShader.FindKernel("GenerateTris");
        longitudeAndLatitudeShader.SetBuffer(kernel, "tris", triangleBuffer);
        longitudeAndLatitudeShader.Dispatch(kernel, Mathf.CeilToInt(triangleBuffer.count / 32f), 1, 1);

        //reading the created triangles into an array
        Vector3Int[] tempTris = new Vector3Int[triangleBuffer.count];
        triangleBuffer.GetData(tempTris);
        DestroyTriangleBuffer();
        int[] tris = Vector3IntsToInts(tempTris);

        Mesh sphere = new Mesh();
        sphere.vertices = points;
        sphere.triangles = tris;
        sphere.RecalculateNormals();
        sphere.RecalculateBounds();
        return sphere;
    }

    //creates a sphere by creating a cube with each face subdivided by a certain amount, then projecting all those points onto a sphere
    public static Mesh CubeSphere(float radius, Vector3 sensorPosition, float[] LODAngles, int numChunkDivisionsPerFace, int chunkSize) {
        Mesh sphere = new Mesh();

        Vector3[] cubeVertexes = new Vector3[8];
        {
            cubeVertexes[0] = new Vector3(1, 1, 1);
            cubeVertexes[1] = new Vector3(1, 1, -1);
            cubeVertexes[2] = new Vector3(1, -1, 1);
            cubeVertexes[3] = new Vector3(1, -1, -1);
            cubeVertexes[4] = new Vector3(-1, 1, 1);
            cubeVertexes[5] = new Vector3(-1, 1, -1);
            cubeVertexes[6] = new Vector3(-1, -1, 1);
            cubeVertexes[7] = new Vector3(-1, -1, -1);
        }

        //defined as 3 vertex indicies of 3 corners of the cube
        Vector3Int[] cubeFaces = new Vector3Int[6];
        {
            cubeFaces[0] = new Vector3Int(0, 1, 4);
            cubeFaces[1] = new Vector3Int(7, 3, 6);
            cubeFaces[2] = new Vector3Int(0, 2, 1);
            cubeFaces[3] = new Vector3Int(7, 6, 5);
            cubeFaces[4] = new Vector3Int(0, 4, 2);
            cubeFaces[5] = new Vector3Int(7, 5, 3);
        }

        CubeChunk[] chunks = new CubeChunk[6 * numChunkDivisionsPerFace * numChunkDivisionsPerFace];
        for (int faceIndex = 0; faceIndex < 6; faceIndex++) {
            Vector3 startingCubeVertex = cubeVertexes[cubeFaces[faceIndex].x];
            Vector3 xIncrement = (cubeVertexes[cubeFaces[faceIndex].y] - startingCubeVertex) / numChunkDivisionsPerFace;
            Vector3 yIncrement = (cubeVertexes[cubeFaces[faceIndex].z] - startingCubeVertex) / numChunkDivisionsPerFace;
            for (int i = 0; i < numChunkDivisionsPerFace; i++) {
                for (int j = 0; j < numChunkDivisionsPerFace; j++) {
                    int chunkIndex = faceIndex * numChunkDivisionsPerFace * numChunkDivisionsPerFace + i * numChunkDivisionsPerFace + j;
                    
                    Vector3[] corners = new Vector3[4];
                    corners[0] = startingCubeVertex + i * yIncrement + j * xIncrement;
                    corners[1] = corners[0] + xIncrement;
                    corners[2] = corners[0] + xIncrement + yIncrement;
                    corners[3] = corners[0] + yIncrement;
                    Vector3 center = Vector3.zero;
                    for (int k = 0; k < 4; k++) {
                        center += corners[k].normalized * radius;
                    }
                    center = center.normalized * radius;

                    float angleToChunk = Vector3.Angle(center, sensorPosition);
                    int lod = LODAngles.Length;
                    for (int k = 0; k < LODAngles.Length; k++) {
                        if(angleToChunk < LODAngles[k]) {
                            lod = k;
                            break;
                        }
                    }

                    chunks[chunkIndex] = new CubeChunk(new Vector3[3] { corners[0], corners[1], corners[3] }, radius, lod, chunkSize);
                }
            }
        }

        int numCountedVerts = 0;
        List<Vector3> verts = new List<Vector3>();
        List<Vector3> norms = new List<Vector3>();
        List<int> tris = new List<int>();

        for (int i = 0; i < chunks.Length; i++) {
            for (int j = 0; j < chunks[i].verts.Length; j++) {
                verts.Add(chunks[i].verts[j]);
                norms.Add(verts[verts.Count - 1].normalized);
            }
            for (int j = 0; j < chunks[i].tris.Length; j++) {
                tris.Add(chunks[i].tris[j] + numCountedVerts);
            }
            numCountedVerts = verts.Count;
        }

        sphere.vertices = verts.ToArray();
        sphere.triangles = tris.ToArray();
        sphere.normals = norms.ToArray();
        sphere.RecalculateBounds();

        return sphere;
    }

    //creates a mesh by creating an icosahedron, subdividing the faces, then projecting the points onto a sphere
    public static Mesh IcoSphere(float radius, int numSubdivisions) {
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        //setting up the basic icosahedron
        //which you can do by creating 3 rectangles in different perpendicular planes to each other with side lengths in the golden ratio
        //and linking up their corners. Here's a picture of the convention I used http://1.bp.blogspot.com/_-FeuT9Vh6rk/Sj1WHbcQwxI/AAAAAAAAABw/xaFDct6AyOI/s400/icopoints.png
        {
            verts.Add(new Vector3(-1, goldenRatio, 0));
            verts.Add(new Vector3(1, goldenRatio, 0));
            verts.Add(new Vector3(-1, -goldenRatio, 0));
            verts.Add(new Vector3(1, -goldenRatio, 0));

            verts.Add(new Vector3(0, -1, goldenRatio));
            verts.Add(new Vector3(0, 1, goldenRatio));
            verts.Add(new Vector3(0, -1, -goldenRatio));
            verts.Add(new Vector3(0, 1, -goldenRatio));

            verts.Add(new Vector3(goldenRatio, 0, -1));
            verts.Add(new Vector3(goldenRatio, 0, 1));
            verts.Add(new Vector3(-goldenRatio, 0, -1));
            verts.Add(new Vector3(-goldenRatio, 0, 1));
        }

        //a list of edges, defined as the 2 indexes of the points the edges connect
        //ordered such that it's the edges within the pentagon shape around vertex 0, the pentagon shape around vertex 3, and then the the remaining edges
        Vector2Int[] edges = new Vector2Int[30];
        {
            edges[0] = new Vector2Int(0, 5);
            edges[1] = new Vector2Int(0, 11);
            edges[2] = new Vector2Int(0, 10);
            edges[3] = new Vector2Int(0, 7);
            edges[4] = new Vector2Int(0, 1);
            edges[5] = new Vector2Int(5, 11);
            edges[6] = new Vector2Int(11, 10);
            edges[7] = new Vector2Int(10, 7);
            edges[8] = new Vector2Int(7, 1);
            edges[9] = new Vector2Int(1, 5);
            edges[10] = new Vector2Int(3, 4);
            edges[11] = new Vector2Int(3, 2);
            edges[12] = new Vector2Int(3, 6);
            edges[13] = new Vector2Int(3, 8);
            edges[14] = new Vector2Int(3, 9);
            edges[15] = new Vector2Int(4, 2);
            edges[16] = new Vector2Int(2, 6);
            edges[17] = new Vector2Int(6, 8);
            edges[18] = new Vector2Int(8, 9);
            edges[19] = new Vector2Int(9, 4);
            edges[20] = new Vector2Int(9, 1);
            edges[21] = new Vector2Int(9, 5);
            edges[22] = new Vector2Int(4, 5);
            edges[23] = new Vector2Int(4, 11);
            edges[24] = new Vector2Int(2, 11);
            edges[25] = new Vector2Int(2, 10);
            edges[26] = new Vector2Int(6, 10);
            edges[27] = new Vector2Int(6, 7);
            edges[28] = new Vector2Int(8, 7);
            edges[29] = new Vector2Int(8, 1);
        }

        //defines the faces of the icosahedron by what edges make up the faces.
        //ordered so that the pentagon around vertex 0 is first, then the faces that share an edge with that pentagon, then the pentagon around vertex 3
        //and the faces that share an edge with that pentagon.
        Vector3Int[] faceToEdges = new Vector3Int[20];
        {
            faceToEdges[0] = new Vector3Int(0, 1, 5);
            faceToEdges[1] = new Vector3Int(1, 2, 6);
            faceToEdges[2] = new Vector3Int(2, 3, 7);
            faceToEdges[3] = new Vector3Int(3, 4, 8);
            faceToEdges[4] = new Vector3Int(4, 0, 9);
            faceToEdges[5] = new Vector3Int(22, 23, 5);
            faceToEdges[6] = new Vector3Int(24, 25, 6);
            faceToEdges[7] = new Vector3Int(26, 27, 7);
            faceToEdges[8] = new Vector3Int(28, 29, 8);
            faceToEdges[9] = new Vector3Int(20, 21, 9);
            faceToEdges[10] = new Vector3Int(10, 11, 15);
            faceToEdges[11] = new Vector3Int(11, 12, 16);
            faceToEdges[12] = new Vector3Int(12, 13, 17);
            faceToEdges[13] = new Vector3Int(13, 14, 18);
            faceToEdges[14] = new Vector3Int(14, 10, 19);
            faceToEdges[15] = new Vector3Int(15, 23, 24);
            faceToEdges[16] = new Vector3Int(16, 25, 26);
            faceToEdges[17] = new Vector3Int(17, 27, 28);
            faceToEdges[18] = new Vector3Int(18, 29, 20);
            faceToEdges[19] = new Vector3Int(19, 21, 22);
        }

        //the way I've got the vertexes sorted, the first 12 are the basic icosahedron vertices, then we add all the vertices that are on the edges of the icosahedron
        //then we add in the vertexes that are in the middle of the faces of the icosahedron

        //inserting all the edge vertices
        for (int i = 0; i < edges.Length; i++) {
            for (int j = 1; j < numSubdivisions + 1; j++) {
                verts.Add(Vector3.Lerp(verts[edges[i].x], verts[edges[i].y], j / ((float)(numSubdivisions + 1))));
            }
        }
        
        for (int i = 0; i < 20; i++) {
            int edgeAIndex = faceToEdges[i].x;
            int edgeBIndex = faceToEdges[i].y;
            int edgeCIndex = faceToEdges[i].z;
            int edgeAVerticesStartIndex = 11 + edgeAIndex * numSubdivisions;
            int edgeBVerticesStartIndex = 11 + edgeBIndex * numSubdivisions;
            int edgeCVerticesStartIndex = 11 + edgeCIndex * numSubdivisions;
            for (int j = 1; j <= numSubdivisions + 1; j++) {
                if (j < numSubdivisions + 1) { // runs for all rows of the vertexes of the face that aren't the top or bottom
                    int startVertexIndex = edgeAVerticesStartIndex + j;
                    int endVertexIndex = edgeBVerticesStartIndex + j;
                    for (int k = 1; k < j; k++) { // will only run for inside vertices
                        verts.Add(Vector3.Lerp(verts[startVertexIndex], verts[endVertexIndex], k / (float)j));
                        int currentVertexIndex = verts.Count - 1;
                        int leftVertexIndex = verts.Count - 2;
                        int leftUpVertexIndex = verts.Count - j;
                        int rightUpVertexIndex = verts.Count - j + 1;
                        if (k == 1) {
                            leftVertexIndex = startVertexIndex;
                            leftUpVertexIndex = startVertexIndex - 1;
                        }
                        if (k == j - 1) {
                            rightUpVertexIndex = endVertexIndex - 1;
                        }
                        //creates the triangles for which the current vertex is the bottom right corner of and bottom middle corner of
                        tris.Add(currentVertexIndex);
                        tris.Add(leftVertexIndex);
                        tris.Add(leftUpVertexIndex);

                        tris.Add(currentVertexIndex);
                        tris.Add(leftUpVertexIndex);
                        tris.Add(rightUpVertexIndex);
                    }
                    //final triangle in the row
                    tris.Add(endVertexIndex);
                    if (j == 1) {
                        tris.Add(startVertexIndex);
                        tris.Add(edges[edgeAIndex].x);
                    } else {
                        tris.Add(verts.Count - 1);
                        tris.Add(endVertexIndex - 1);
                    }
                } else {//bottommost row of vertexes in the face
                    for (int k = 1; k <= numSubdivisions; k++) {
                        int currentVertexIndex = edgeCVerticesStartIndex + k;
                        int leftVertexIndex = edgeCVerticesStartIndex + k - 1;
                        int leftUpVertexIndex = verts.Count - 1 - (numSubdivisions - k);
                        int rightUpVertexIndex = verts.Count - (numSubdivisions - k);
                        if (k == 1) {
                            leftVertexIndex = edges[edgeCIndex].x;
                            leftUpVertexIndex = edgeAVerticesStartIndex + j - 1;
                        }
                        if (k == j - 1) {
                            rightUpVertexIndex = edgeBVerticesStartIndex + j - 1;
                        }
                        //creates the triangles for which the current vertex is the bottom right corner of and bottom middle corner of
                        tris.Add(currentVertexIndex);
                        tris.Add(leftVertexIndex);
                        tris.Add(leftUpVertexIndex);

                        tris.Add(currentVertexIndex);
                        tris.Add(leftUpVertexIndex);
                        tris.Add(rightUpVertexIndex);
                    }
                    //final triangle in the row
                    tris.Add(edges[edgeCIndex].y);
                    if (j == 1) {
                        tris.Add(edges[edgeCIndex].x);
                        tris.Add(edges[edgeAIndex].x);
                    } else {
                        tris.Add(edgeCVerticesStartIndex + numSubdivisions);
                        tris.Add(edgeBVerticesStartIndex + numSubdivisions);
                    }
                }
            }
        }
        
        //projecting all the points onto a sphere
        SetupPointBufferForCommonOperations(verts);
        commonOperationsShader.SetFloat("radius", radius);
        int kernel = commonOperationsShader.FindKernel("ProjectOntoSphere");
        commonOperationsShader.SetBuffer(kernel, "points", pointBuffer);
        commonOperationsShader.Dispatch(kernel, Mathf.CeilToInt(verts.Count/32f), 1, 1);

        //flipping all the triangles so they face outwards
        SetupTriangleBufferForCommonOperations(tris);
        kernel = commonOperationsShader.FindKernel("FlipTriangles");
        commonOperationsShader.SetBuffer(kernel, "points", pointBuffer);
        commonOperationsShader.SetBuffer(kernel, "tris", triangleBuffer);
        commonOperationsShader.Dispatch(kernel, Mathf.CeilToInt(triangleBuffer.count / 32f), 1, 1);

        Mesh sphere = new Mesh();
        //reading data from our buffers to our mesh
        Vector3[] whyIsThisNecessary = new Vector3[verts.Count]; // for some reason it doesn't like me reading the point data directly from the buffer to the mesh, requiring this extra step
        pointBuffer.GetData(whyIsThisNecessary);
        sphere.vertices = whyIsThisNecessary;
        Vector3Int[] tempTris = new Vector3Int[triangleBuffer.count];
        triangleBuffer.GetData(tempTris);
        sphere.triangles = Vector3IntsToInts(tempTris);

        DestroyPointBuffer();
        DestroyTriangleBuffer();

        sphere.RecalculateNormals();
        sphere.RecalculateBounds();

        return sphere;
    }
    
    //creates a fibbonacci sphere with a given number of points
    public static Mesh FibonacciSphere(float radius, int numPoints) {
        Mesh sphere = new Mesh();
        Vector3[] verts = new Vector3[numPoints];
        Vector2[] stereographicPoints = new Vector2[numPoints - 1]; //from south pole

        float phi = 2 * Mathf.PI * goldenRatio;

        for (int i = 0; i < numPoints; i++) {
            float y = 2 * i / (numPoints - 1f) - 1; //goes from -1 to 1 (south pole to north pole)
            float scaledRadius = Mathf.Sqrt(1 - y * y);
            float angle = phi * i;

            float x = Mathf.Cos(angle) * scaledRadius;
            float z = Mathf.Sin(angle) * scaledRadius;
            verts[i] = new Vector3(x, y, z) * radius;

            if (i != 0) {
                stereographicPoints[i - 1] = StereographicProjection(verts[i], verts[0], radius);
            }
        }
        //very slow for larger data sets
        List<int> tris = DelaunayTriangulation(stereographicPoints);

        List<Vector2Int> edges = new List<Vector2Int>();
        for (int i = 0; i < tris.Count; i+= 3) {
            tris[i]++;
            tris[i + 1]++;
            tris[i + 2]++;
            edges.Add(new Vector2Int(tris[i], tris[i + 1]));
            edges.Add(new Vector2Int(tris[i + 1], tris[i + 2]));
            edges.Add(new Vector2Int(tris[i + 2], tris[i]));
        }

        Vector2Int[] uniqueEdges = GetUniqueEdges(edges);
        
        for (int i = 0; i < uniqueEdges.Length; i++) {
            tris.Add(0);
            tris.Add(uniqueEdges[i].x);
            tris.Add(uniqueEdges[i].y);
        }

        //flipping all the triangles so they face outwards
        SetupTriangleBufferForCommonOperations(tris);
        SetupPointBufferForCommonOperations(verts);
        int kernel = commonOperationsShader.FindKernel("FlipTriangles");
        commonOperationsShader.SetBuffer(kernel, "points", pointBuffer);
        commonOperationsShader.SetBuffer(kernel, "tris", triangleBuffer);
        commonOperationsShader.Dispatch(kernel, Mathf.CeilToInt(triangleBuffer.count / 32f), 1, 1);

        sphere.vertices = verts;
        //reading data from our buffer to our mesh
        Vector3Int[] tempTris = new Vector3Int[triangleBuffer.count];
        triangleBuffer.GetData(tempTris);
        sphere.triangles = Vector3IntsToInts(tempTris);

        DestroyPointBuffer();
        DestroyTriangleBuffer();

        sphere.RecalculateNormals();
        sphere.RecalculateBounds();

        return sphere;
    }

    //not a sphere, but good to have
    public static Mesh FibonacciSpiral(float radius, int numPoints, float pointRadius) {
        Mesh spiral = new Mesh();

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        Mesh ball = LongitudeAndLatitiudeSphere(pointRadius, 10, 50);
        int triangleStartIndex = 0;

        float phi = 2 * Mathf.PI * goldenRatio;

        for (int i = 0; i < numPoints; i++) {
            float distFromCenter = i * radius / (numPoints - 1f);
            float angle = phi * i;

            float x = Mathf.Cos(angle) * distFromCenter;
            float z = Mathf.Sin(angle) * distFromCenter;

            for (int j = 0; j < ball.vertexCount; j++) {
                verts.Add(ball.vertices[j] + new Vector3(x, 0, z));
            }
            for (int j = 0; j < ball.triangles.Length; j++) {
                tris.Add(ball.triangles[j] + triangleStartIndex);
            }
            triangleStartIndex = verts.Count;
        }

        spiral.vertices = verts.ToArray();
        spiral.triangles = tris.ToArray();
        spiral.RecalculateNormals();
        spiral.RecalculateBounds();

        return spiral;
    }

    //HELPER FUNCTIONS BELOW HERE
    private static Vector3 ProjectOntoSphere(Vector3 point, float radius) {
        return point.normalized * radius;
    }

    private static Vector2 StereographicProjection(Vector3 point, Vector3 projectionPoint, float radius) { // from south pole
        Vector3 projectionVector = point - projectionPoint;
        Vector3.Normalize(projectionVector);
        float cosineOfAngleBetween = Vector3.Dot(Vector3.up, projectionVector);
        projectionVector *= 2 * radius / cosineOfAngleBetween;
        Vector3 projection3D = projectionPoint + projectionVector;
        return new Vector2(projection3D.x, projection3D.z);
    }

    private static List<int> DelaunayTriangulation(Vector2[] points) {
        List<Vector3Int> triangleList = new List<Vector3Int>();
        Vector2[] workingPointsList = new Vector2[points.Length + 3];
        points.CopyTo(workingPointsList, 0);

        //getting the 3 points of the super triangle
        float maxSqrRadius = 0;
        for (int i = 0; i < points.Length; i++) {
            if (points[i].sqrMagnitude > maxSqrRadius) {
                maxSqrRadius = points[i].sqrMagnitude;
            }
        }

        //inserting the super triangle points at the end of our points list
        float radius = Mathf.Sqrt(maxSqrRadius) * 2;
        List<int> superVertexIndicies = new List<int>();
        for (int i = 0; i < 3; i++) {
            float theta = 2 * Mathf.PI / 3 * i;
            workingPointsList[points.Length + i] = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta)) * radius;
            superVertexIndicies.Add(points.Length + i);
        }

        triangleList.Add(new Vector3Int(points.Length, points.Length + 1, points.Length + 2));
        
        for (int i = 0; i < points.Length; i++) {
            //finding triangles that invalidate the delaunay conditions
            List<Vector3Int> invalidTriangles = new List<Vector3Int>();
            for (int j = triangleList.Count - 1; j >= 0; j--) {
                Vector3 circumcircle = GetCircumcircleOfTriangle(triangleList[j], workingPointsList);
                Vector2 pointToCircle = new Vector2(circumcircle.x - points[i].x, circumcircle.y - points[i].y);
                if(pointToCircle.sqrMagnitude <= circumcircle.z) {
                    invalidTriangles.Add(triangleList[j]);
                    triangleList.RemoveAt(j);
                }
            }

            //finding the points that form the polygon we now have to triangulate (because a hole just opened up in our triangulation)
            List<Vector2Int> edges = new List<Vector2Int>();
            for (int j = 0; j < invalidTriangles.Count; j++) {
                edges.Add(new Vector2Int(invalidTriangles[j].x, invalidTriangles[j].y));
                edges.Add(new Vector2Int(invalidTriangles[j].y, invalidTriangles[j].z));
                edges.Add(new Vector2Int(invalidTriangles[j].z, invalidTriangles[j].x));
            }

            Vector2Int[] uniqueEdges = GetUniqueEdges(edges);

            //retriangulating the polygon
            for (int j = 0; j < uniqueEdges.Length; j++) {
                triangleList.Add(new Vector3Int(uniqueEdges[j].x, uniqueEdges[j].y, i));
            }
        }

        //getting rid of triangles that include the super triangle vertices
        for (int i = triangleList.Count - 1; i >= 0; i--) {
            if (superVertexIndicies.Contains(triangleList[i].x) || superVertexIndicies.Contains(triangleList[i].y) || superVertexIndicies.Contains(triangleList[i].z)) {
                triangleList.RemoveAt(i);
            }
        }

        return Vector3IntsToInts(triangleList);
    }

    private static List<int> Vector3IntsToInts(List<Vector3Int> vecs) {
        List<int> ints = new List<int>();
        for (int i = 0; i < vecs.Count; i++) {
            ints.Add(vecs[i].x);
            ints.Add(vecs[i].y);
            ints.Add(vecs[i].z);
        }
        return ints;
    }

    private static int[] Vector3IntsToInts(Vector3Int[] vecs) {
        int[] ints = new int[vecs.Length * 3];
        for (int i = 0; i < vecs.Length; i++) {
            ints[i * 3] = vecs[i].x;
            ints[i * 3 + 1] = vecs[i].y;
            ints[i * 3 + 2] = vecs[i].z;
        }
        return ints;
    }

    private static Vector3 GetCircumcircleOfTriangle(Vector3Int triangleIndexes, Vector2[] points) {
        //returns the circumcircle in the format (x,y,square magnitude of radius)

        //to get the x and y of the center circumcircle, you construct 2 perpendicular bisectors of 2 sides of the triangle and find where they meet
        //the size of the circumcircle is just the distance from the center of the circumcircle to one of the points
        //I found the center by solving (on paper) where the intersection of the bisectors of 2 arbitrary sides of an arbitrary triangle are
        //by using B+BC/2+t*normBC as the equations for the bisector where BC was the vector from B to C, normBC was that vector rotated 90 degrees,
        //and t being the parametric variable. I constructed a similar equation for the other bisector and solved for t
        //This gives the equation for t which you see below, and I then just plugged that back into the equation to get the center
        Vector2 pointA = points[triangleIndexes.x];
        Vector2 pointB = points[triangleIndexes.y];
        Vector2 pointC = points[triangleIndexes.z];
        Vector2 AB = pointB - pointA;
        Vector2 BC = pointC - pointB;
        float t = (AB.sqrMagnitude + Vector2.Dot(AB,BC)) / (2 * (BC.x * AB.y - AB.x * BC.y));
        float x = pointB.x + BC.x / 2 + t * BC.y;
        float y = pointB.y + BC.y / 2 - t * BC.x;
        float z = (pointA - new Vector2(x,y)).sqrMagnitude;
        return new Vector3(x, y, z);
    }

    private static Vector2Int[] GetUniqueEdges(List<Vector2Int> edges) {
        int numUniqueFound = 0;
        for (int i = edges.Count - 1; i >= 0; i--) {
            Vector2Int edge = edges[i];
            Predicate<Vector2Int> isEdge = (Vector2Int a) => { return ((a.x == edge.x && a.y == edge.y) || (a.x == edge.y && a.y == edge.x)); };
            List<Vector2Int> matchingEdges = edges.FindAll(isEdge);
            if (matchingEdges.Count > 1) {
                edges.RemoveAll(isEdge);
                i = edges.Count - 1 - numUniqueFound;
            } else {
                numUniqueFound++;
            }
        }
        return edges.ToArray();
    }

    //functions for compute shader stuff
    private static void SetupPointBufferForCommonOperations(List<Vector3> verts) {
        if (pointBuffer == null || !pointBuffer.IsValid() || pointBuffer.count != verts.Count) {
            pointBuffer = new ComputeBuffer(verts.Count, 12);
        }
        pointBuffer.SetData(verts);
    }

    private static void SetupPointBufferForCommonOperations(Vector3[] verts) {
        if (pointBuffer == null || !pointBuffer.IsValid() || pointBuffer.count != verts.Length) {
            pointBuffer = new ComputeBuffer(verts.Length, 12);
        }
        pointBuffer.SetData(verts);
    }

    private static void SetupTriangleBufferForCommonOperations(List<int> tris) {
        if (triangleBuffer == null || !triangleBuffer.IsValid() || triangleBuffer.count != tris.Count / 3) {
            triangleBuffer = new ComputeBuffer(tris.Count / 3, 12);
        }
        triangleBuffer.SetData(tris);
    }

    private static void SetupPointBufferForLongitudeAndLatitudeSphere(int longitudeLines, int latitudeLines) {
        if (pointBuffer == null || !pointBuffer.IsValid() || pointBuffer.count != longitudeLines * latitudeLines) {
            pointBuffer = new ComputeBuffer(longitudeLines * latitudeLines, 12);
        }
    }

    private static void SetupTriangleBufferForLongitudeAndLatitudeSphere(int longitudeLines, int latitudeLines) {
        if (triangleBuffer == null || !triangleBuffer.IsValid() || triangleBuffer.count != 2 * longitudeLines + (latitudeLines - 1) * longitudeLines * 2) {
            triangleBuffer = new ComputeBuffer(2 * longitudeLines + (latitudeLines - 1) * longitudeLines * 2, 12);
        }
    }

    private static void DestroyPointBuffer() {
        if (pointBuffer != null) {
            pointBuffer.Release();
        }
    }

    private static void DestroyTriangleBuffer() {
        if (triangleBuffer != null) {
            triangleBuffer.Release();
        }
    }
    
    private struct CubeChunk {
        public Vector3[] verts;
        public int[] tris;

        //diagram https://www.desmos.com/calculator/bcagu8b0vn
        public CubeChunk(Vector3[] corners, float radius, int lod, int chunkSize) {
            int numPointsPerLine = chunkSize + 3;
            int skipIncrement = lod == 0 ? 1 : lod * 2;

            int[,] vertexIndexMap = new int[numPointsPerLine, numPointsPerLine];
            int vertexNum = 0;
            for (int i = 0; i < numPointsPerLine; i++) {
                for (int j = 0; j < numPointsPerLine; j++) {
                    bool isSkippedVertex = i > 1 && j > 1 && i < numPointsPerLine - 2 && j < numPointsPerLine - 2 && ((i - 1) % skipIncrement != 0 || (j - 1) % skipIncrement != 0);
                    if (!isSkippedVertex) {
                        vertexIndexMap[j, i] = vertexNum;
                        vertexNum++;
                    }
                }
            }

            verts = new Vector3[vertexNum];
            List<int> tempTris = new List<int>();

            for (int i = 0; i < numPointsPerLine; i++) {
                for (int j = 0; j < numPointsPerLine; j++) {
                    int triSize = 1;
                    bool createTris = false;
                    int vertexIndex = vertexIndexMap[j, i];
                    if (i == 0 || i == numPointsPerLine - 1 || j == 0 || j == numPointsPerLine - 1) {
                        //mesh edge vertex
                        verts[vertexIndex] = ProjectOntoSphere(cubeVertexFromCornerAndIndexes(corners, j, i, numPointsPerLine), radius);
                        if (i != 0 && j != 0) {//add a set of edge connection triangles
                            createTris = true;
                        }
                    } else if ((i - 1) % skipIncrement == 0 && (j - 1) % skipIncrement == 0) {
                        //main vertex
                        verts[vertexIndex] = ProjectOntoSphere(cubeVertexFromCornerAndIndexes(corners, j, i, numPointsPerLine), radius);

                        createTris = true;
                        if (i != 1 && j != 1) { //add a set of main triangles
                            triSize = skipIncrement;
                        }
                    } else if (i == 1 || i == numPointsPerLine - 2 || j == 1 || j == numPointsPerLine - 2) {
                        //edge connection vertex
                        if (j == 1 || j == numPointsPerLine - 2) { //needs to interpolate between the main vertices above and below it
                            Vector3 topVertex = ProjectOntoSphere(cubeVertexFromCornerAndIndexes(corners, j, i - ((i - 1) % skipIncrement), numPointsPerLine), radius);
                            Vector3 bottomVertex = ProjectOntoSphere(cubeVertexFromCornerAndIndexes(corners, j, i + (skipIncrement - ((i - 1) % skipIncrement)), numPointsPerLine), radius);
                            verts[vertexIndex] = Vector3.Lerp(topVertex, bottomVertex, (i - 1) % skipIncrement / (float)skipIncrement);
                        } else { //needs to interpolate between the main vertices to either side of it
                            Vector3 leftVertex = ProjectOntoSphere(cubeVertexFromCornerAndIndexes(corners, j - ((j - 1) % skipIncrement), i, numPointsPerLine), radius);
                            Vector3 rightVertex = ProjectOntoSphere(cubeVertexFromCornerAndIndexes(corners, j + (skipIncrement - ((j - 1) % skipIncrement)), i, numPointsPerLine), radius);
                            verts[vertexIndex] = Vector3.Lerp(leftVertex, rightVertex, (j - 1) % skipIncrement / (float)skipIncrement);
                        }
                        if (i == 1 || j == 1) {//add a set of edge connection triangles
                            createTris = true;
                        }
                    }
                    if (createTris) {
                        tempTris.Add(vertexIndex);
                        tempTris.Add(vertexIndexMap[j - triSize, i]);
                        tempTris.Add(vertexIndexMap[j - triSize, i - triSize]);

                        tempTris.Add(vertexIndex);
                        tempTris.Add(vertexIndexMap[j - triSize, i - triSize]);
                        tempTris.Add(vertexIndexMap[j, i - triSize]);
                    }
                }
            }

            tris = tempTris.ToArray();
        }

        private static Vector3 cubeVertexFromCornerAndIndexes(Vector3[] corners, int x, int y, int numPointsPerLine) {
            return corners[0] + ((corners[1] - corners[0]) / (numPointsPerLine - 1f) * x) + ((corners[2] - corners[0]) / (numPointsPerLine - 1f) * y);
        }
    }
}
