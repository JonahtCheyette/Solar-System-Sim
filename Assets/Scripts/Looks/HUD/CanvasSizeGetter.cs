using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasSizeGetter : MonoBehaviour {
    RectTransform self;
    // Start is called before the first frame update
    void Start() {
        self = gameObject.GetComponent<RectTransform>();
    }

    public Vector2 getSize() {
        return new Vector2(self.rect.width, self.rect.height);
    }
}
