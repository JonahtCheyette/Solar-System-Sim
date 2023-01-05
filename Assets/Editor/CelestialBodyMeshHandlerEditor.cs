using UnityEngine;
using UnityEditor;

//says to use this editor for the CelestialBodyMeshHandler Script
[CustomEditor(typeof(CelestialBodyMeshHandler))]
public class CelestialBodyMeshHandlerEditor : Editor {
    CelestialBodyMeshHandler chunkHandler;

    bool shapeFoldout;
    Editor shapeEditor;

    bool shaderFoldout;
    Editor shaderEditor;

    // Start is called before the first frame update
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        // Draw shape/shading object editors
        DrawCachedEditor(chunkHandler.celestialBodyGenerator, ref shapeFoldout, ref shapeEditor);
        DrawCachedEditor(chunkHandler.shaderDataGenerator, ref shaderFoldout, ref shaderEditor);

        if (GUILayout.Button("Generate")) {
            chunkHandler.Generate();
        }
        if (GUILayout.Button("Regenerate Mesh values")) {
            chunkHandler.SetLowRezMeshValues();
        }

        SaveState();
    }

    void DrawCachedEditor(Object settings, ref bool foldout, ref Editor editor) {
        if (settings != null) {
            foldout = EditorGUILayout.InspectorTitlebar(foldout, settings);
            if (foldout) {
                CreateCachedEditor(settings, null, ref editor);
                editor.OnInspectorGUI();
            }
        }
    }

    private void OnEnable() {
        //gets the CelestialBodyGenerator reference
        chunkHandler = (CelestialBodyMeshHandler)target;

        shapeFoldout = EditorPrefs.GetBool(nameof(shapeFoldout), false);
        shaderFoldout = EditorPrefs.GetBool(nameof(shaderFoldout), false);
    }

    void SaveState() {
        EditorPrefs.SetBool(nameof(shapeFoldout), shapeFoldout);
        EditorPrefs.SetBool(nameof(shaderFoldout), shaderFoldout);
    }
}
