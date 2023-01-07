using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MovementPrediction))]
public class MovementPredictionEditor : Editor {

    public override void OnInspectorGUI() {
        //gets the BestTerrainHandler reference
        MovementPrediction movementPrediction = (MovementPrediction)target;
        DrawDefaultInspector();

        //creates a button that calls GenerateChunks when Pressed
        if (GUILayout.Button("Predict")) {
            movementPrediction.PredictMovement();
        }
    }
}
