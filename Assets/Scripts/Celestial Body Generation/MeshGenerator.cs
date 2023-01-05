using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator {
    public static MeshData GenerateMeshData(int chunkSize, int lod, Vector3[] verts, Vector4[,]uvs, bool[] cornersOnEdgeOfFace) {
        int skipIncrement = lod == 0 ? 1 : lod * 2;

        List<int> tris = new List<int>();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector4>[] UV = new List<Vector4>[uvs.GetUpperBound(1) + 1];
        for (int i = 0; i < UV.Length; i++) {
            UV[i] = new List<Vector4>();
        }

        int[] vertIndexMap = new int[verts.Length];

        //this set of for loops determines which vertices we actually have to calculate stuff for, and sets up an array that takes in
        //the vertIndex(just consecutively how many vertices we've gone through) and spits out the index in the vertices list and therefore mesh
        //because we don't necessarily put each vertex we've gone through into the mesh
        //we also set up the normal & uv list here
        int vertIndex = 0;
        int verticesIndex = 0;
        for (int y = 1; y < chunkSize + 6; y++) {// how far down (up in the case of the upside down chunks) the triangle we are
            for (int x = 0; x < y + 1; x++) {// how far left (right in the case of the upside down chunks) the triangle we are
                /* I should note that from here on in the variable names assume right-side up chunks.
                 * everything still works the same on the upside down chunks, so this is just for human convinience when trying to picture stuff
                 *      ^
                 *     /  \ right side up
                 *    <____>
                 * 
                 *    <---->
                 *     \  / upside-down
                 *      V
                 */
                if (!(cornersOnEdgeOfFace[0] && y == 1 && x == 0)) {// we skip everything in the case that the vertex is over the edge of the main icosahedron face, luckily this is the only corner we have to check for
                    bool isMainVertex = y > 3 && y < chunkSize + 5 && x > 1 && x < y - 1 && (y - 4) % skipIncrement == 0 && (x - 2) % skipIncrement == 0;
                    bool isSkippedVertex = y > 4 && y < chunkSize + 4 && x > 2 && x < y - 2 && !isMainVertex;
                    if (!isSkippedVertex) {
                        if (y > 1 && x > 0 && x < y) {
                            vertIndexMap[vertIndex] = verticesIndex;
                            verticesIndex++;
                            normals.Add(Vector3.zero);
                            for (int i = 0; i < UV.Length; i++) {// this looks weird, but it has to be this way because UV is the transpose of uvs
                                UV[i].Add(uvs[vertIndex, i]);
                            }
                        }
                    }
                    vertIndex++;
                }
            }
        }

        //these for loops do the actual processing of the vertices
        vertIndex = 0;
        for (int y = 1; y < chunkSize + 6; y++) {//these for loops (and if statement) do the same things as the ones above
            for (int x = 0; x < y + 1; x++) {
                if (!(cornersOnEdgeOfFace[0] && y == 1 && x == 0)) { 
                    bool isInMainTriangle = y > 3 && y < chunkSize + 5 && x > 1 && x < y - 1; // is in the main triangle (the part of the chunk that LOD effects). Note that this includes the vertices that are on the edge of this "Main triangle"
                    bool isMainVertex = isInMainTriangle && (y - 4) % skipIncrement == 0 && (x - 2) % skipIncrement == 0; // A vertex in the main triangle that LOD effects
                    bool isSkippedVertex = y > 4 && y < chunkSize + 4 && x > 2 && x < y - 2 && !isMainVertex; // A vertex in the main triangle that we skip over due to LOD
                    bool isLeftEdgeConnectionVertex = isInMainTriangle && !isSkippedVertex && !isMainVertex && x == y - 2; // a vertex in the main triangle that we must include in order to get the normals along the edge of the main triangle to work
                    bool isBottomEdgeConnectionVertex = isInMainTriangle && !isSkippedVertex && !isMainVertex && y == chunkSize + 4;// same as above
                    if (!isSkippedVertex) {
                        /* These arrays track whether the vertices in the triangles we're concerned with for this vertex are in the mesh
                         * and what vertIndex each vertex would have
                         * ordering of arrays is top right, bottom right, bottom left, top right, bottom left, top left
                         * the first triangle uses the first 3, the second triangle uses the last 3.
                         *  X---X
                         *   \ / \
                         *    X---X
                         *  X = vertex
                         *  I should note that the top right vertex is the current vertex
                         */
                        bool[] vertexInMesh = new bool[6] { y > 1 && x > 0 && x < y, x > 0 && y < chunkSize + 5, x < y && y < chunkSize + 5, y > 1 && x > 0 && x < y, x < y && y < chunkSize + 5, x < y - 1 };
                        int[] vertsIndex = new int[6] { vertIndex, vertIndex + y + 1, vertIndex + y + 2, vertIndex, vertIndex + y + 2, vertIndex + 1 };

                        // assuming an icosahedral face orientated the same was as our right-side up chunks, is the vertex in question on one of the corners
                        bool topVertexOfFace = vertIndex == 0 && cornersOnEdgeOfFace[0]; 
                        bool leftVertexOfFace = cornersOnEdgeOfFace[2] && y == chunkSize + 5 && x == y;
                        bool rightVertexOfFace = cornersOnEdgeOfFace[1] && y == chunkSize + 5 && x == 0;

                        //the increment between the vertex and the bottom right vertex of that vertex's first main triangle (the triangles that are effected by LOD)
                        int downRightMainTriangleInc = skipIncrement * y + skipIncrement * (skipIncrement + 1) / 2;

                        if (vertexInMesh[0]) {
                            if (isInMainTriangle && !isMainVertex) { // for edge connection vertices, calculating where they are based on the vertices in the main triangle they're in between
                                Vector3 vertex1;
                                Vector3 vertex2;
                                float percentBetween;
                                if (isLeftEdgeConnectionVertex) {
                                    int numRowsUp = (y - 4) % skipIncrement;
                                    int numRowsDown = skipIncrement - numRowsUp;
                                    vertex1 = verts[vertIndex - y * numRowsUp + numRowsUp * (numRowsUp - 1) / 2 - numRowsUp];
                                    vertex2 = verts[vertIndex + y * numRowsDown + numRowsDown * (numRowsDown + 1) / 2 + numRowsDown];
                                    percentBetween = ((y - 4) % skipIncrement) / (float)skipIncrement;
                                } else if (isBottomEdgeConnectionVertex) {
                                    vertex1 = verts[vertIndex - ((x - 2) % skipIncrement)];
                                    vertex2 = verts[vertIndex + skipIncrement - ((x - 2) % skipIncrement)];
                                    percentBetween = ((x - 2) % skipIncrement) / (float)skipIncrement;
                                } else {
                                    int numRowsUp = (y - 4) % skipIncrement;
                                    int numRowsDown = skipIncrement - numRowsUp;
                                    vertex1 = verts[vertIndex - y * numRowsUp + numRowsUp * (numRowsUp  - 1) / 2];
                                    vertex2 = verts[vertIndex + y * numRowsDown + numRowsDown * (numRowsDown + 1) / 2];
                                    percentBetween = ((y - 4) % skipIncrement) / (float)skipIncrement;
                                }
                                vertices.Add(Vector3.Lerp(vertex1, vertex2, percentBetween));
                            } else {
                                vertices.Add(verts[vertIndex]);
                            }
                        }

                        if (isMainVertex && y < chunkSize + 4) { //the vertex indicies for the main triangles
                            vertsIndex[1] = vertIndex + downRightMainTriangleInc;
                            vertsIndex[2] = vertsIndex[1] + skipIncrement;
                            if (x != y - 2) {
                                vertsIndex[4] = vertsIndex[2];
                                vertsIndex[5] = vertIndex + skipIncrement;
                            }
                        }

                        if (y == chunkSize + 5) {//we have to change the vert indexes for the triangles on the bottom that aren't included in the final mesh due to the corners of the triangular grid not mattering
                            vertsIndex[1]--;
                            vertsIndex[2]--;
                            vertsIndex[4]--;
                            if (cornersOnEdgeOfFace[1]) { // if the bottom left corner of the chunk happens to be the bottom left corner of the main icosahedron face, we have to shift vertices one more over
                                vertsIndex[1]--;
                                vertsIndex[2]--;
                                vertsIndex[4]--;
                            }
                        }
                        if (topVertexOfFace) { // special shuffling we have to do for the top, left, and right vertices of the face respectively
                            vertsIndex[5] = 3;
                            vertsIndex[1] = 1;
                            vertsIndex[2] = 2;
                            vertsIndex[4] = 2;
                            vertexInMesh[1] = false;
                            vertexInMesh[2] = true;
                            vertexInMesh[4] = true;
                        }
                        if (rightVertexOfFace) {
                            vertsIndex[2]++;
                            vertsIndex[4]++;
                        }
                        if (leftVertexOfFace) {
                            vertsIndex[1] = vertIndex - 1;
                            vertsIndex[2] = verts.Length - 1;
                            vertsIndex[4] = verts.Length - 1;
                            vertexInMesh[1] = true;
                        }

                        //do we include a given triangle in the normal caclculations
                        bool doFirstTriangle = true;
                        bool doSecondTriangle = true;
                        if (isInMainTriangle && !isMainVertex && !isBottomEdgeConnectionVertex) {
                            doFirstTriangle = false;
                            doSecondTriangle = false;
                            if (isLeftEdgeConnectionVertex) {
                                doSecondTriangle = true;
                            }
                        }
                        if (cornersOnEdgeOfFace[2] && y == chunkSize + 5 && x == y - 1) {//this makes it so that wrong triangles aren't calculated due to there being missing vertices if the chunk is on the bottom left corner of the face
                            doFirstTriangle = false;
                            doSecondTriangle = false;
                        }
                        if (cornersOnEdgeOfFace[1] && y == chunkSize + 5 && x == 1) {// this makes it so that wrong triangles aren't calculated due to there being missing vertices if the chunk is on the bottom right corner of the face
                            doFirstTriangle = false;
                        }
                        if (!leftVertexOfFace && (y == chunkSize + 5 && (x == 0 || x == y))) {// we need this triangle if we're working with the leftmost vertex in the face. Otherwise, vertices on the bottom corner don't need this triangle
                            doFirstTriangle = false;
                        }
                        if (!topVertexOfFace && x == y) {// if we're currently working with the top vertex, then we need both triangles. Otherwise, if this is the last vertex for which we are calculating triangles for this row, this triangle would be wrong
                            doSecondTriangle = false;
                        }

                        if (doFirstTriangle) {
                            if (vertexInMesh[0] && vertexInMesh[1] && vertexInMesh[2]) { // should we add this triangle to the mesh
                                for (int k = 0; k < 3; k++) {
                                    tris.Add(vertIndexMap[vertsIndex[k]]);
                                }
                            }
                            //adding the weighted normal to the normals for each vertex
                            Vector3 weightedNormal = triangleWeightedNormal(verts[vertsIndex[0]], verts[vertsIndex[1]], verts[vertsIndex[2]]);
                            for (int k = 0; k < 3; k++) {
                                if (vertexInMesh[k]) { normals[vertIndexMap[vertsIndex[k]]] += weightedNormal; }
                            }
                            if (isMainVertex && y != chunkSize + 4) {//if we're on the bottom edge of the main triangle, It's guaranteed we don't need to do anything special for the first triangle
                                //however, if this triangle is on the edges of the main triangle, we need to add the normal to the relevant edge connection vertices
                                if (x == 2) {//right side of our triangle
                                    int index = vertsIndex[0];
                                    for (int k = 1; k < skipIncrement; k++) {
                                        index += y + k;
                                        normals[vertIndexMap[index]] += weightedNormal;
                                    }
                                }
                                if (x == y - 2) {//left side of our triangle
                                    int index = vertsIndex[0];
                                    for (int k = 1; k < skipIncrement; k++) {
                                        index += y + k + 1;
                                        normals[vertIndexMap[index]] += weightedNormal;
                                    }
                                }
                                if (y == chunkSize + 4 - skipIncrement) {//bottom of our triangle
                                    int index = vertsIndex[1];
                                    for (int k = 1; k < skipIncrement; k++) {
                                        index += 1;
                                        normals[vertIndexMap[index]] += weightedNormal;
                                    }
                                }
                            }
                        }
                        if (doSecondTriangle) {
                            if (vertexInMesh[3] && vertexInMesh[4] && vertexInMesh[5]) { // should we add this triangle to the mesh
                                for (int k = 3; k < 6; k++) {
                                    tris.Add(vertIndexMap[vertsIndex[k]]);
                                }
                            }
                            //the code for adding these weighted normals is much simpler
                            Vector3 weightedNormal = triangleWeightedNormal(verts[vertsIndex[3]], verts[vertsIndex[4]], verts[vertsIndex[5]]);
                            for (int k = 3; k < 6; k++) {
                                if (vertexInMesh[k]) { normals[vertIndexMap[vertsIndex[k]]] += weightedNormal; }
                            }
                        }
                    }
                    vertIndex++;
                }
            }
        }

        List<Vector4> tangents = new List<Vector4>();

        //normalizing everything and calculating the tangents
        for (int i = 0; i < normals.Count; i++) {
            normals[i] = normals[i].normalized;
            tangents.Add(tangentFromNormal(normals[i])); // I REALLY Want to know whether compute shaders are thread-safe
        }

        return new MeshData(vertices.ToArray(), normals.ToArray(), tangents.ToArray(), UV, tris.ToArray());
    }

    private static Vector3 triangleWeightedNormal(Vector3 a, Vector3 b, Vector3 c) {
        return Vector3.Cross(b - a, c - a);// not negative despite triangles being in clockwise order because unity uses a left handed coordinate system
        //now the weighted normal is supposed to have a length equal to the area of the triangle, which is given by Vector3.Cross(b - a, c - a) / 2
        //However, I think I don't need to divide by 2 because the normals are currently all done by this function, meaning that getting rid of this /2
        //simply scale up each normal by 2 but then they get fed to a normalize function, which gives the same result either way
    }

    public static Vector4 tangentFromNormal(Vector3 n) {
        Vector3 t = Vector3.Normalize(Vector3.Cross(n, n == Vector3.down || n == Vector3.up ? Vector3.back : Vector3.down));
        return new Vector4(t.x, t.y, t.z, 1);
    }
}

public class MeshData {
    private Vector3[] verts;
    private Vector3[] norms;
    private Vector4[] tangents;
    private Vector4[][] uvs;
    private int[] tris;

    public MeshData (Vector3[] v, Vector3[] n, Vector4[] tans, List<Vector4>[] u, int[] t) {
        verts = v;
        norms = n;
        tangents = tans;
        tris = t;
        uvs = new Vector4[u.Length][];
        for (int i = 0; i < u.Length; i++) {
            uvs[i] = u[i].ToArray();
        }
    }

    public Mesh createMesh() {
        Mesh m = new Mesh();
        m.vertices = verts;
        m.triangles = tris;
        m.normals = norms;
        m.tangents = tangents;
        for (int i = 0; i < uvs.Length; i++) {
            m.SetUVs(i, uvs[i]);
        }

        m.RecalculateBounds();
        return m;
    }
}