using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SimulationCloth : MonoBehaviour
{
    [SerializeField]
    Material materialCloth;

    public void UpdateSimulationCamera(SimulationCamera sim)
    {
        GetComponent<MeshFilter>().sharedMesh = sim.ClothMesh;
        materialCloth.mainTexture = sim.PositionTexture;
    }

    void Awake()
    {
        GetComponent<MeshRenderer>().material = materialCloth;
    }
}
