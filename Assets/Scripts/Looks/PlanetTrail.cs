using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//VERY IMPORTANT: THIS CODE DOESN'T CURRENTLY WORK, IF YOU WANT TO SEE WHY CHECK THE UpdateTrails FUNCTION

[ImageEffectAllowedInSceneView] [ExecuteAlways] // have to use ExecuteAlways, otherwise I get a warning when coming back from play mode
public class PlanetTrail : MonoBehaviour {
    private CelestialBody[] bodies;

    private List<TrailSegment> fullSegmentList; // the full list of positions all the planets have been in
    private List<PlanetPos> fullPlanetList;
    private List<TrailSegmentData> segmentsToBeSentToShader;
    // since I already do all the pruning and ordering of planet data in full Planet list, 
    // this list's only purpose is converting the PlanetPos to PlanetData, which I could probably use Linq for
    private List<PlanetData> planetsToBeSentToShader;
    private List<Vector3> planetColors; //will be sent to the shader, the int named planetIndex/index in the various structs we have is simply the index of their color in this list
    
    private ComputeBuffer planetBuffer; // buffers for the various data we send to the shader
    private ComputeBuffer segmentBuffer;
    private ComputeBuffer numSegBuffer;
    private ComputeBuffer colBuffer;

    private Vector3[] oldPlanetPositions; // the position of the planets last frame, in world space
    private int[] totalSegsOfPlanet; // the number of segments being rendered to the screen for each planet

    private bool firstFrame;
    private Material mat;
    private Shader shader;
    private Camera cam;

    // current aspect ratio of the screen. needed because bottom left is always (0, 0) and topright is always (1, 1), no matter what the actual screen dimensions are
    private float aspectRatio;

    [Min(0)]
    public int maxLength = 1000; // maximum length of fullSegmentList
    [Min(0)]
    public float haloRadiusMultiplier = 2;

    private void Start() {
        bodies = FindObjectsOfType<CelestialBody>();

        fullSegmentList = new List<TrailSegment>();
        fullPlanetList = new List<PlanetPos>();
        segmentsToBeSentToShader = new List<TrailSegmentData>();
        planetsToBeSentToShader = new List<PlanetData>();

        planetColors = new List<Vector3>();
        foreach (CelestialBody body in bodies) { // getting the planet color data
            planetColors.Add(new Vector3(body.color.r, body.color.g, body.color.b));
        }

        oldPlanetPositions = new Vector3[bodies.Length];
        totalSegsOfPlanet = new int[bodies.Length];

        shader = Shader.Find("Hidden/PlanetTrail");
        cam = Camera.current;
        firstFrame = true;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        if (Camera.current != null && Application.isPlaying && Camera.current != Camera.main) {
            // makes it so that the image effect only runs in scene view during play mode
            GetAspectRatioAndCamera(source);
            ResetLists();
            UpdateCelstialBodyData();
            FinalizeData();
            SetUpBuffers();
            SetBufferData();
            Render(source, destination);
        } else {
            Graphics.Blit(source, destination);
        }
    }

    private void GetAspectRatioAndCamera(RenderTexture source) {
        aspectRatio = (source.width) / ((float)source.height);
        cam = Camera.current;
    }

    private void ResetLists() {
        segmentsToBeSentToShader.Clear();
        planetsToBeSentToShader.Clear();
        fullPlanetList.Clear();
    }

    private void UpdateCelstialBodyData() {
        for (int i = 0; i < bodies.Length; i++) {
            UpdatePlanetList(i);
            UpdateTrails(i);
        }
    }

    private void UpdatePlanetList(int bodyIndex) {
        //getting the position and radius of the planet in viewportSpace
        Vector3 viewportPosition = cam.WorldToViewportPoint(bodies[bodyIndex].Position);
        float radius = cam.WorldToViewportPoint(bodies[bodyIndex].Position + cam.transform.up * bodies[bodyIndex].radius).y - viewportPosition.y;

        PlanetPos currentPlanet = new PlanetPos(viewportPosition, bodyIndex, radius);
        if (currentPlanet.OnScreen(aspectRatio, haloRadiusMultiplier)) {
            if (fullPlanetList.Count == 0) {
                fullPlanetList.Add(currentPlanet);
            } else {
                //ordering the planets from furthest away to closest;
                for (int i = 0; i < fullPlanetList.Count; i++) {
                    if (viewportPosition.z < fullPlanetList[i].Depth) {
                        fullPlanetList.Insert(i, currentPlanet);
                        break;
                    }
                    if (i == fullPlanetList.Count - 1) {
                        fullPlanetList.Add(currentPlanet);
                        break; // important! if this isn't here, this will cause a memory leak
                    }
                }
            }
        }
    }

    private void UpdateTrails(int bodyIndex) {
        totalSegsOfPlanet[bodyIndex] = 0;
        if (!firstFrame) { // since we don't have the planet's old body Positions on the first frame, we shouldn't make any trail segments
            // I'm commenting out this code, since the celestialbody class no longer holds data on it's previous positions since last frame,
            // since it was only really necessary for making these trails work
            /*
            Vector3 oldPosition = oldPlanetPositions[bodyIndex];
            for (int i = 0; i < bodies[bodyIndex].positionsSinceLastFrame.Count; i++) { 
                // loop through all the celestial bodies positions since last frame and make segments for each, to prevent the trail from looking choppy
                fullSegmentList.Add(new TrailSegment(oldPosition, bodies[bodyIndex].positionsSinceLastFrame[i], bodyIndex, bodies[bodyIndex].radius));
                oldPosition = bodies[bodyIndex].positionsSinceLastFrame[i];
            }
            fullSegmentList.Add(new TrailSegment(oldPosition, bodies[bodyIndex].Position, bodyIndex, bodies[bodyIndex].radius));

            // kinda weird that I had to clear this data here, and the fact that it was necesary for this system to work is a testament to how bad it was
            bodies[bodyIndex].ClearPositions(); 
            */
            // making sure we aren't storing too many positions to handle
            while (fullSegmentList.Count > maxLength) {
                fullSegmentList.RemoveAt(0);
            }
        }
    }

    private void FinalizeData() {
        //making sure the amount of data we pass to the shader is as minimal as possible, 
        //PlanetData struct doesn't have depth value since they're already ordered
        foreach (PlanetPos planet in fullPlanetList) {
            planetsToBeSentToShader.Add(new PlanetData(planet));
        }
        
        if (firstFrame) {
            firstFrame = false;
        } else {
            //trailsegmentdata is the segment, but with the coords and thickness converted to viewport space
            foreach (TrailSegment segment in fullSegmentList) {
                TrailSegmentData data = new TrailSegmentData(segment, cam);
                if (data.OnScreen(aspectRatio)) {
                    segmentsToBeSentToShader.Add(data);
                    totalSegsOfPlanet[data.PlanetIndex]++;
                }
            }
        }

        for (int i = 0; i < bodies.Length; i++) {
            oldPlanetPositions[i] = bodies[i].Position;
        }
    }

    private void SetUpBuffers() {
        if (planetBuffer == null) {
            planetBuffer = new ComputeBuffer(Mathf.Max(planetsToBeSentToShader.Count, 1), PlanetData.GetSize());
        } else if (planetBuffer.count != planetsToBeSentToShader.Count) {
            planetBuffer.Release();
            planetBuffer = new ComputeBuffer(Mathf.Max(planetsToBeSentToShader.Count, 1), PlanetData.GetSize());
        }
        
        if (segmentBuffer == null) {
            segmentBuffer = new ComputeBuffer(Mathf.Max(segmentsToBeSentToShader.Count, 1), TrailSegmentData.GetSize());
        } else if (segmentBuffer.count != segmentsToBeSentToShader.Count) {
            segmentBuffer.Release();
            segmentBuffer = new ComputeBuffer(Mathf.Max(segmentsToBeSentToShader.Count, 1), TrailSegmentData.GetSize());
        }

        if (numSegBuffer == null) {
            numSegBuffer = new ComputeBuffer(Mathf.Max(totalSegsOfPlanet.Length, 1), sizeof(int));
        } else if (numSegBuffer.count != segmentsToBeSentToShader.Count) {
            numSegBuffer.Release();
            numSegBuffer = new ComputeBuffer(Mathf.Max(totalSegsOfPlanet.Length, 1), sizeof(int));
        }
    }

    private void SetBufferData() {
        planetBuffer.SetData(planetsToBeSentToShader.ToArray());
        segmentBuffer.SetData(segmentsToBeSentToShader.ToArray());
        numSegBuffer.SetData(totalSegsOfPlanet);
    }

    private void Render(RenderTexture src, RenderTexture dest) {
        if (planetsToBeSentToShader.Count == 0 && segmentsToBeSentToShader.Count == 0 && shader != null) {
            Graphics.Blit(src, dest); // don't do any fancy effects, because you don't need to.
            return;
        }
        if (mat == null || mat.shader == null || mat.shader != shader) {
            mat = new Material(shader);
            SetStaticMaterialValues();
        }
        
        SetDynamicMaterialValues();
        Graphics.Blit(src, dest, mat);
    }

    private void SetStaticMaterialValues() {
        // I considered seting this up in the same way as all the other buffers,
        // but considering that the planetColors list literally never changes
        // I think it's fine doing it this way, considering that the fact that this function 
        // should only be called once, meaning that we aren't wasting time checking that this buffer exists
        // and re - setting the data in it every frame when the data in it isn't changing anyways
        if (colBuffer == null) {
            colBuffer = new ComputeBuffer(planetColors.Count, sizeof(float) * 3);
            colBuffer.SetData(planetColors.ToArray());
        }
        mat.SetBuffer("colors", colBuffer);
    }

    private void SetDynamicMaterialValues() {
        mat.SetBuffer("segments", segmentBuffer);
        mat.SetBuffer("planets", planetBuffer);
        mat.SetInt("numSegments", segmentsToBeSentToShader.Count);
        mat.SetInt("numPlanets", planetsToBeSentToShader.Count);
        mat.SetBuffer("totalSegsOfPlanet", numSegBuffer); // part of my ill-fated task of getting the trails to look how I wanted
        mat.SetFloat("planetHaloSize", haloRadiusMultiplier);
    }

    private void OnDisable() {
        DestroyBuffers();
    }

    private void OnApplicationQuit() {
        DestroyBuffers();
    }

    private void OnDestroy() {
        DestroyBuffers();
    }

    private void DestroyBuffers() {
        if (planetBuffer != null) {
            planetBuffer.Release();
        }
        if (segmentBuffer != null) {
            segmentBuffer.Release();
        }
        if (colBuffer != null) {
            colBuffer.Release();
        }
        if (numSegBuffer != null) {
            numSegBuffer.Release();
        }
    }

    private struct TrailSegment {
        private Vector3 start; // where the segment starts and ends in world space
        private Vector3 end;
        private int planetIndex;
        private float radius; // the thickness of the segment in world space

        public TrailSegment(Vector3 begin, Vector3 finish, int i, float size) {
            start = begin;
            end = finish;
            planetIndex = i;
            radius = size;
        }

        public Vector2 GetViewportPosition(Camera c, bool startingPosition) {
            Vector3 vPos = c.WorldToViewportPoint(startingPosition ? start : end);
            return new Vector2(vPos.x, vPos.y);
        }

        public int PlanetIndex {
            get {
                return planetIndex;
            }
        }

        public float GetViewportRadius (Camera c) {
            Vector3 midPosition = (start + end) / 2f;
            return c.WorldToViewportPoint(midPosition + c.transform.up * radius).y - c.WorldToViewportPoint(midPosition).y;
        }
    }

    private struct TrailSegmentData { // to be sent to the shader
        private Vector2 start; // where the segment starts and ends in viewportSpace
        private Vector2 end;
        private int planetIndex;
        private float thickness; //  the thickness of the segment in viewportSpace

        public TrailSegmentData (TrailSegment segment, Camera c) {
            start = segment.GetViewportPosition(c, true);
            end = segment.GetViewportPosition(c, false);
            planetIndex = segment.PlanetIndex;
            thickness = segment.GetViewportRadius(c);
        }

        private float GetSqrDstToSegment(Vector2 p, float aRatio) {
            Vector2 offset = end - start;
            float sqrSegLength = Vector2.Dot(offset, offset);
            float t = Mathf.Max(0, Mathf.Min(1, Vector2.Dot(p - start, offset) / sqrSegLength));
            Vector2 projection = start + t * offset; //where p is projected to be on the line defined by start and end, the closest point on that line
            Vector2 offsetToSegment = p - projection;
            offsetToSegment.x *= aRatio;
            return Vector2.Dot(offsetToSegment, offsetToSegment); //the distance squared!
        }

        public bool OnScreen(float aRatio) {
            if((end.x * aRatio >= -thickness && end.x <= 1 + (thickness/aRatio) && end.y >= -thickness && end.y <= 1 + thickness) || (start.x * aRatio >= -thickness && start.x <= 1 + (thickness / aRatio) && start.y >= -thickness && start.y <= 1 + thickness)) {
                return true;
            }
            
            for (int i = 0; i < 4; i++) {
                if(GetSqrDstToSegment(new Vector2(Mathf.FloorToInt(i/2), i % 2), aRatio) <= thickness * thickness) {
                    return true;
                }
            }

            //this technically will return false if the segment is like this:
            //
            //      ___________________
            //      |       SCREEN     |
            //   ---+------------------+---
            //      |                  | 
            //      --------------------
            // where both ends are too far off-screen, but the only way I can picture this happening is if the segment is behind the camera
            // and we shouldn't be rendering those anyways, Not to mention adding a check for this case would make this function even slower
            
            return false;
        }

        public int PlanetIndex {
            get {
                return planetIndex;
            }
        }

        public static int GetSize() {
            return sizeof(int) + 5 * sizeof(float);
        }
    }

    private struct PlanetPos {
        private Vector2 viewportPos;
        private float depth;
        private int index;
        private float radius;

        public PlanetPos (Vector3 pos, int i, float r) {
            viewportPos = new Vector2(pos.x, pos.y);
            depth = pos.z;
            index = i;
            radius = r;
        }
        
        public float Depth {
            get {
                return depth;
            }
        }
        
        public Vector2 ViewportPos {
            get {
                return viewportPos;
            }
        }
        
        public int Index {
            get {
                return index;
            }
        }

        public float Radius {
            get {
                return radius;
            }
        }

        public bool OnScreen(float aRatio, float radiusMultiplier) {
            return viewportPos.x * aRatio >= -radius * radiusMultiplier && viewportPos.x <= 1 + (radius * radiusMultiplier / aRatio) && viewportPos.y >= -radius * radiusMultiplier && viewportPos.y <= 1 + (radius * radiusMultiplier);
        }
    }

    private struct PlanetData {
        private Vector2 viewportPos;
        private int index;
        private float radius;

        public PlanetData(PlanetPos planet) {
            viewportPos = planet.ViewportPos;
            index = planet.Index;
            radius = planet.Radius;
        }

        public static int GetSize (){
            return sizeof(int) + 3 * sizeof(float);
        }
    }
}