using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WorleyGenerator))]
public class WorleyGeneratorEditor : Editor {
    SerializedProperty autoUpdate;

    SerializedProperty radius;
    SerializedProperty seed;

    SerializedProperty useOcean;
    SerializedProperty oceanRadiusBoost;
    SerializedProperty oceanShallowColor;
    SerializedProperty oceanDeepColor;
    SerializedProperty oceanBlendMultiplier;
    SerializedProperty oceanAlphaMultiplier;

    SerializedProperty numWorleyPoints;
    SerializedProperty amountPerturbed;
    SerializedProperty worleyThreshold;
    SerializedProperty worleyBlend;
    SerializedProperty worleyBoostMinMax;
    SerializedProperty worleyMultiplierMinMax;

    GUIContent oceanGUI = new GUIContent("Ocean Settings");
    GUIContent worleyGUI = new GUIContent("Worley Settings");

    bool oceanDropdown;
    bool worleyDropdown;

    void OnEnable() {
        autoUpdate = serializedObject.FindProperty("autoUpdate");

        radius = serializedObject.FindProperty("radius");
        seed = serializedObject.FindProperty("seed");

        useOcean = serializedObject.FindProperty("useOcean");
        oceanRadiusBoost = serializedObject.FindProperty("oceanRadiusBoost");
        oceanShallowColor = serializedObject.FindProperty("oceanShallowColor");
        oceanDeepColor = serializedObject.FindProperty("oceanDeepColor");
        oceanBlendMultiplier = serializedObject.FindProperty("oceanBlendMultiplier");
        oceanAlphaMultiplier = serializedObject.FindProperty("oceanAlphaMultiplier");

        numWorleyPoints = serializedObject.FindProperty("numWorleyPoints");
        amountPerturbed = serializedObject.FindProperty("amountPerturbed");
        worleyThreshold = serializedObject.FindProperty("worleyThreshold");
        worleyBlend = serializedObject.FindProperty("worleyBlend");
        worleyBoostMinMax = serializedObject.FindProperty("worleyBoostMinMax");
        worleyMultiplierMinMax = serializedObject.FindProperty("worleyMultiplierMinMax");

        oceanDropdown = EditorPrefs.GetBool(nameof(oceanDropdown), false);
        worleyDropdown = EditorPrefs.GetBool(nameof(worleyDropdown), false);
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
            EditorGUILayout.PropertyField(useOcean);
            EditorGUILayout.PropertyField(oceanRadiusBoost);
            EditorGUILayout.PropertyField(oceanShallowColor);
            EditorGUILayout.PropertyField(oceanDeepColor);
            EditorGUILayout.PropertyField(oceanBlendMultiplier);
            EditorGUILayout.PropertyField(oceanAlphaMultiplier);
        }

        if (EditorGUILayout.DropdownButton(worleyGUI, FocusType.Keyboard)) {
            worleyDropdown = !worleyDropdown;
        }

        if (worleyDropdown) {
            EditorGUILayout.PropertyField(numWorleyPoints);
            EditorGUILayout.PropertyField(amountPerturbed);
            EditorGUILayout.PropertyField(worleyThreshold);
            EditorGUILayout.PropertyField(worleyBlend);
            EditorGUILayout.PropertyField(worleyBoostMinMax);
            EditorGUILayout.PropertyField(worleyMultiplierMinMax);
        }
        SaveState();

        serializedObject.ApplyModifiedProperties();
    }

    void SaveState() {
        EditorPrefs.SetBool(nameof(oceanDropdown), oceanDropdown);
        EditorPrefs.SetBool(nameof(worleyDropdown), worleyDropdown);
    }
}
