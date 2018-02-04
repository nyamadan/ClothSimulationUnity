using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClothSimulation : MonoBehaviour {
    [SerializeField]
    Transform sphere;

    private void Update()
    {
        var pos = sphere.localPosition;
        pos.z = 4.0f * Mathf.Cos(Time.time);
        sphere.localPosition = pos;
    }
}
