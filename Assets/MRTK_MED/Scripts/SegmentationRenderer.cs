using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SegmentationRenderer : MonoBehaviour
{
    public Mesh myMesh; // Assign your mesh in the Inspector or load it programmatically
    public Material urpMaterial; // Assign your URP-compatible material in the Inspector

    void Start()
    {
        // Add MeshFilter and Renderer2D components to the new GameObject
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();

        // Assign the provided mesh to the MeshFilter's mesh
        meshFilter.mesh = myMesh;

        // Assign the URP material to the Renderer2D
        meshRenderer.material = urpMaterial;

        // Set the position, rotation, and scale of the new GameObject as needed
        gameObject.transform.position = Vector3.zero; // Set position
        gameObject.transform.rotation = Quaternion.identity; // Set rotation
        gameObject.transform.localScale = Vector3.one/1000.0f; // Set scale
    }
}
