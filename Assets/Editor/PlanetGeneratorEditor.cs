using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlanetGenerator))]
public class PlanetGeneratorEditor : Editor {
    SerializedProperty autoUpdate;

    SerializedProperty radius;
    SerializedProperty seed;

    SerializedProperty oceanShallowColor;
    SerializedProperty oceanDeepColor;
    SerializedProperty oceanBlendMultiplier;
    SerializedProperty oceanAlphaMultiplier;

    SerializedProperty oceanFloorDepth;
    SerializedProperty oceanDamper;
    SerializedProperty oceanSmoothing;
    SerializedProperty oceanDepthMultiplier;

    SerializedProperty genShapeScale;
    SerializedProperty genShapePersistance;
    SerializedProperty genShapeLacunarity;
    SerializedProperty genShapeOctaves;
    SerializedProperty genShapeMultiplier;

    SerializedProperty ridgeScale;
    SerializedProperty ridgePersistance;
    SerializedProperty ridgeLacunarity;
    SerializedProperty ridgeOctaves;
    SerializedProperty ridgeMultiplier;
    SerializedProperty ridgeGain;
    SerializedProperty ridgeSharpness;

    SerializedProperty mountainMaskOffset;
    SerializedProperty mountainMaskShift;
    SerializedProperty mountainMaskScale;

    GUIContent oceanGUI = new GUIContent("Ocean Settings");
    GUIContent genShapeGUI = new GUIContent("General Shape Settings");
    GUIContent ridgeGUI = new GUIContent("Ridge Settings");
    GUIContent mountainMaskGUI = new GUIContent("MountainMask Settings");

    bool oceanDropdown;
    bool genShapeDropdown;
    bool ridgeDropdown;
    bool mountainMaskDropDown;

    void OnEnable() {
        autoUpdate = serializedObject.FindProperty("autoUpdate");

        radius = serializedObject.FindProperty("radius");
        seed = serializedObject.FindProperty("seed");

        oceanShallowColor = serializedObject.FindProperty("oceanShallowColor");
        oceanDeepColor = serializedObject.FindProperty("oceanDeepColor");
        oceanBlendMultiplier = serializedObject.FindProperty("oceanBlendMultiplier");
        oceanAlphaMultiplier = serializedObject.FindProperty("oceanAlphaMultiplier");

        oceanFloorDepth = serializedObject.FindProperty("oceanFloorDepth");
        oceanDamper = serializedObject.FindProperty("oceanDamper");
        oceanSmoothing = serializedObject.FindProperty("oceanSmoothing");
        oceanDepthMultiplier = serializedObject.FindProperty("oceanDepthMultiplier");

        genShapeScale = serializedObject.FindProperty("genShapeScale");
        genShapePersistance = serializedObject.FindProperty("genShapePersistance");
        genShapeLacunarity = serializedObject.FindProperty("genShapeLacunarity");
        genShapeOctaves = serializedObject.FindProperty("genShapeOctaves");
        genShapeMultiplier = serializedObject.FindProperty("genShapeMultiplier");

        ridgeScale = serializedObject.FindProperty("ridgeScale");
        ridgePersistance = serializedObject.FindProperty("ridgePersistance");
        ridgeLacunarity = serializedObject.FindProperty("ridgeLacunarity");
        ridgeOctaves = serializedObject.FindProperty("ridgeOctaves");
        ridgeMultiplier = serializedObject.FindProperty("ridgeMultiplier");
        ridgeGain = serializedObject.FindProperty("ridgeGain");
        ridgeSharpness = serializedObject.FindProperty("ridgeSharpness");

        mountainMaskOffset = serializedObject.FindProperty("mountainMaskOffset");
        mountainMaskShift = serializedObject.FindProperty("mountainMaskShift");
        mountainMaskScale = serializedObject.FindProperty("mountainMaskScale");

        oceanDropdown = EditorPrefs.GetBool(nameof(oceanDropdown), false);
        genShapeDropdown = EditorPrefs.GetBool(nameof(genShapeDropdown), false);
        ridgeDropdown = EditorPrefs.GetBool(nameof(ridgeDropdown), false);
        mountainMaskDropDown = EditorPrefs.GetBool(nameof(mountainMaskDropDown), false);
    }

    public override void OnInspectorGUI() {
        serializedObject.Update();

        EditorGUILayout.PropertyField(autoUpdate);
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(seed);
        EditorGUILayout.PropertyField(radius);

        if (EditorGUILayout.DropdownButton(oceanGUI, FocusType.Keyboard)) {
            oceanDropdown = !oceanDropdown;
        }

        if (oceanDropdown) {
            EditorGUILayout.PropertyField(oceanShallowColor);
            EditorGUILayout.PropertyField(oceanDeepColor);
            EditorGUILayout.PropertyField(oceanBlendMultiplier);
            EditorGUILayout.PropertyField(oceanAlphaMultiplier);
            EditorGUILayout.PropertyField(oceanFloorDepth);
            EditorGUILayout.PropertyField(oceanDamper);
            EditorGUILayout.PropertyField(oceanSmoothing);
            EditorGUILayout.PropertyField(oceanDepthMultiplier);
        }

        if (EditorGUILayout.DropdownButton(genShapeGUI, FocusType.Keyboard)) {
            genShapeDropdown = !genShapeDropdown;
        }

        if (genShapeDropdown) {
            EditorGUILayout.PropertyField(genShapeMultiplier);
            EditorGUILayout.PropertyField(genShapeOctaves);
            EditorGUILayout.PropertyField(genShapeScale);
            EditorGUILayout.PropertyField(genShapePersistance);
            EditorGUILayout.PropertyField(genShapeLacunarity);
        }

        if (EditorGUILayout.DropdownButton(ridgeGUI, FocusType.Keyboard)) {
            ridgeDropdown = !ridgeDropdown;
        }

        if (ridgeDropdown) {
            EditorGUILayout.PropertyField(ridgeMultiplier);
            EditorGUILayout.PropertyField(ridgeOctaves);
            EditorGUILayout.PropertyField(ridgeScale);
            EditorGUILayout.PropertyField(ridgePersistance);
            EditorGUILayout.PropertyField(ridgeLacunarity);
            EditorGUILayout.PropertyField(ridgeGain);
            EditorGUILayout.PropertyField(ridgeSharpness);

            if (EditorGUILayout.DropdownButton(mountainMaskGUI, FocusType.Keyboard)) {
                mountainMaskDropDown = !mountainMaskDropDown;
            }

            if (mountainMaskDropDown) {
                EditorGUILayout.PropertyField(mountainMaskOffset);
                EditorGUILayout.PropertyField(mountainMaskShift);
                EditorGUILayout.PropertyField(mountainMaskScale);
            }
        }

        SaveState();

        serializedObject.ApplyModifiedProperties();
    }

    void SaveState() {
        EditorPrefs.SetBool(nameof(oceanDropdown), oceanDropdown);
        EditorPrefs.SetBool(nameof(genShapeDropdown), genShapeDropdown);
        EditorPrefs.SetBool(nameof(ridgeDropdown), ridgeDropdown);
        EditorPrefs.SetBool(nameof(mountainMaskDropDown), mountainMaskDropDown);
    }
}