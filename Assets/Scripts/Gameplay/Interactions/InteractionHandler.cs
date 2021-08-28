using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class InteractionHandler {
    // Handles keeping track and displaying what the player can interact with
    private static List<Interaction> interactionsInRange = new List<Interaction>();

    private static Text interactionText;
    private static Vector2 canvasSize;
    private static float textColor = 0;

    private static Transform playerTransform;

    public static void Initialize(Transform player) {
        interactionText = GameObject.Find("Interact Text").GetComponent<Text>();
        canvasSize = interactionText.GetComponentInParent<CanvasScaler>().referenceResolution;
        RectTransform textTransform = interactionText.GetComponent<RectTransform>();
        // setting the position/size of the text window
        textTransform.anchorMin = Vector2.one * 0.5f;
        textTransform.anchorMax = Vector2.one * 0.5f;
        textTransform.pivot = Vector2.one * 0.5f;
        textTransform.sizeDelta = new Vector2(canvasSize.x, 150);
        textTransform.anchoredPosition = new Vector2(0, -canvasSize.y * 0.375f);
        // setting the basic text settings
        interactionText.text = "";
        interactionText.fontSize = 44;
        interactionText.alignment = TextAnchor.MiddleCenter;
        
        playerTransform = player;
    }

    public static void RunInteractions() {
        DrawInteractionPrompts();
        CheckIfInteracting();
        interactionsInRange.Clear();
    }

    public static void AddInteractionIfInRange(System.Action interact, string interactName, KeyCode interactkey, Vector3 position, float minInteractionRange) {
        // adds the interaction to interactionsInRange if it's within minInteractionRange
        // and not already in the list
        // if there's another interaction that uses the same key, whichever one is closest 
        // will be the one that remains in interactionsInRange
        // Also limits the number of interactions to 3
        float dist = (playerTransform.position - position).magnitude;
        if (dist < minInteractionRange) {
            for(int i = 0; i < interactionsInRange.Count; i++) {
                if (interactionsInRange[i].Key == interactkey && interactionsInRange[i].Dist > dist) {// found an interaction with the same key
                    interactionsInRange.RemoveAt(i);
                    interactionsInRange.Insert(i, new Interaction(interact, interactName, interactkey, dist));
                    return;
                }
            }

            if (interactionsInRange.Count < 3) {
                interactionsInRange.Add(new Interaction(interact, interactName, interactkey, dist));
            } else {// find the interaction with the furthest distance from the player, then remove it
                float maxDist = dist;
                int index = 4;

                for (int i = 0; i < interactionsInRange.Count; i++) {
                    if (interactionsInRange[i].Dist > maxDist) {// found an interaction that's farther away
                        maxDist = interactionsInRange[i].Dist;
                        index = i;
                    }
                }

                // if the interaction is closer than the furthest interaction in the list, replace the furthest interaction
                if (index != 4) {
                    interactionsInRange.RemoveAt(index);
                    interactionsInRange.Add(new Interaction(interact, interactName, interactkey, dist));
                }
            }
        }
    }

    private static void DrawInteractionPrompts() {
        interactionText.text = "";
        for (int i = 0; i < interactionsInRange.Count; i++) {
            if(i != 0) {
                interactionText.text += "\n";
            }
            interactionText.text += interactionsInRange[i].GetMessage();
        }
        AnimateTextColor();
    }

    private static void AnimateTextColor() {
        textColor += 0.03f;
        textColor %= Mathf.PI * 2f;
        float greyScaleValue = (230f + (20f * Mathf.Sin(textColor))) / 255f;
        interactionText.color = new Color(greyScaleValue, greyScaleValue, greyScaleValue);
    }

    private static void CheckIfInteracting() {
        foreach (Interaction interaction in interactionsInRange) {
            if (Input.GetKeyDown(interaction.Key)) {
                interaction.Invoke();
                return;
            }
        }
    }

    private struct Interaction {
        private System.Action interaction;
        private string name;
        private KeyCode key;
        public float dist;

        public Interaction(System.Action interact, string n, KeyCode k, float d) {
            interaction = interact;
            name = n;
            key = k;
            dist = d;
        }

        public void Invoke() {
            interaction();
        }

        public string GetMessage() {
            return $"Press {key} to {name}";
        }

        public KeyCode Key {
            get {
                return key;
            }
        }

        public float Dist {
            get {
                return dist;
            }
        }
    }
}
