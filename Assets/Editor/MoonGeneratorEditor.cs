using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MoonGenerator))]
public class MoonGeneratorEditor : Editor {
    SerializedProperty autoUpdate;

    SerializedProperty radius;
    SerializedProperty seed;

    SerializedProperty numCraters;
    SerializedProperty craterRadMinMax;
    SerializedProperty rimSteepnessMinMax;
    SerializedProperty rimWidthMinMax;

    SerializedProperty craterSmoothing;
    SerializedProperty craterMultiplier;

    SerializedProperty genShapeScale;
    SerializedProperty genShapePersistance;
    SerializedProperty genShapeLacunarity;
    SerializedProperty genShapeOctaves;
    SerializedProperty genShapeMultiplier;

    SerializedProperty detailScale;
    SerializedProperty detailPersistance;
    SerializedProperty detailLacunarity;
    SerializedProperty detailOctaves;
    SerializedProperty detailMultiplier;

    SerializedProperty ridgeScale;
    SerializedProperty ridgePersistance;
    SerializedProperty ridgeLacunarity;
    SerializedProperty ridgeOctaves;
    SerializedProperty ridgeMultiplier;
    SerializedProperty ridgeGain;
    SerializedProperty ridgeSharpness;

    GUIContent craterGUI = new GUIContent("Crater Settings");
    GUIContent genShapeGUI = new GUIContent("General Shape Settings");
    GUIContent detailGUI = new GUIContent("Detail Settings");
    GUIContent ridgeGUI = new GUIContent("Ridge Settings");

    bool craterDropdown;
    bool genShapeDropdown;
    bool detailDropdown;
    bool ridgeDropdown;

    void OnEnable() {
        autoUpdate = serializedObject.FindProperty("autoUpdate");

        radius = serializedObject.FindProperty("radius");
        seed = serializedObject.FindProperty("seed");

        numCraters = serializedObject.FindProperty("numCraters");
        craterRadMinMax = serializedObject.FindProperty("craterRadMinMax");
        rimSteepnessMinMax = serializedObject.FindProperty("rimSteepnessMinMax");
        rimWidthMinMax = serializedObject.FindProperty("rimWidthMinMax");

        craterSmoothing = serializedObject.FindProperty("craterSmoothing");
        craterMultiplier = serializedObject.FindProperty("craterMultiplier");

        genShapeScale = serializedObject.FindProperty("genShapeScale");
        genShapePersistance = serializedObject.FindProperty("genShapePersistance");
        genShapeLacunarity = serializedObject.FindProperty("genShapeLacunarity");
        genShapeOctaves = serializedObject.FindProperty("genShapeOctaves");
        genShapeMultiplier = serializedObject.FindProperty("genShapeMultiplier");

        detailScale = serializedObject.FindProperty("detailScale");
        detailPersistance = serializedObject.FindProperty("detailPersistance");
        detailLacunarity = serializedObject.FindProperty("detailLacunarity");
        detailOctaves = serializedObject.FindProperty("detailOctaves");
        detailMultiplier = serializedObject.FindProperty("detailMultiplier");

        ridgeScale = serializedObject.FindProperty("ridgeScale");
        ridgePersistance = serializedObject.FindProperty("ridgePersistance");
        ridgeLacunarity = serializedObject.FindProperty("ridgeLacunarity");
        ridgeOctaves = serializedObject.FindProperty("ridgeOctaves");
        ridgeMultiplier = serializedObject.FindProperty("ridgeMultiplier");
        ridgeGain = serializedObject.FindProperty("ridgeGain");
        ridgeSharpness = serializedObject.FindProperty("ridgeSharpness");

        craterDropdown = EditorPrefs.GetBool(nameof(craterDropdown), false);
        genShapeDropdown = EditorPrefs.GetBool(nameof(genShapeDropdown), false);
        detailDropdown = EditorPrefs.GetBool(nameof(detailDropdown), false);
        ridgeDropdown = EditorPrefs.GetBool(nameof(ridgeDropdown), false);
    }

    public override void OnInspectorGUI() {
        serializedObject.Update();

        EditorGUILayout.PropertyField(autoUpdate);
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(seed);
        EditorGUILayout.PropertyField(radius);

        if (EditorGUILayout.DropdownButton(craterGUI, FocusType.Keyboard)) {
            craterDropdown = !craterDropdown;
        }

        if (craterDropdown) {
            EditorGUILayout.PropertyField(numCraters);
            EditorGUILayout.PropertyField(craterMultiplier);
            EditorGUILayout.PropertyField(craterSmoothing);
            EditorGUILayout.PropertyField(craterRadMinMax);
            EditorGUILayout.PropertyField(rimSteepnessMinMax);
            EditorGUILayout.PropertyField(rimWidthMinMax);
        }

        if(EditorGUILayout.DropdownButton(genShapeGUI, FocusType.Keyboard)) {
            genShapeDropdown = !genShapeDropdown;
        }

        if (genShapeDropdown) {
            EditorGUILayout.PropertyField(genShapeMultiplier);
            EditorGUILayout.PropertyField(genShapeOctaves);
            EditorGUILayout.PropertyField(genShapeScale);
            EditorGUILayout.PropertyField(genShapePersistance);
            EditorGUILayout.PropertyField(genShapeLacunarity);
        }

        if (EditorGUILayout.DropdownButton(detailGUI, FocusType.Keyboard)) {
            detailDropdown = !detailDropdown;
        }

        if (detailDropdown) {
            EditorGUILayout.PropertyField(detailMultiplier);
            EditorGUILayout.PropertyField(detailOctaves);
            EditorGUILayout.PropertyField(detailScale);
            EditorGUILayout.PropertyField(detailPersistance);
            EditorGUILayout.PropertyField(detailLacunarity);
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
        }

        SaveState();

        serializedObject.ApplyModifiedProperties();
    }

    void SaveState() {
        EditorPrefs.SetBool(nameof(craterDropdown), craterDropdown);
        EditorPrefs.SetBool(nameof(genShapeDropdown), genShapeDropdown);
        EditorPrefs.SetBool(nameof(detailDropdown), detailDropdown);
        EditorPrefs.SetBool(nameof(ridgeDropdown), ridgeDropdown);
    }
}
