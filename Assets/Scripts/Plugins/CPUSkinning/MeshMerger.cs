using UnityEngine;

public class MeshMerger : MonoBehaviour
{
    public SkinnedMeshRenderer[] meshRenderers; // Array of all SkinnedMeshRenderers to merge

    void Start()
    {
        MergeMeshes();
    }

    void MergeMeshes()
    {
        // Combine instance arrays
        CombineInstance[] combineInstances = new CombineInstance[meshRenderers.Length];
        Matrix4x4[] boneMatrices = new Matrix4x4[meshRenderers.Length];
        Transform[] newBones = new Transform[meshRenderers.Length];

        for (int i = 0; i < meshRenderers.Length; i++)
        {
            SkinnedMeshRenderer smr = meshRenderers[i];

            // For mesh
            combineInstances[i].mesh = smr.sharedMesh;
            combineInstances[i].transform = smr.transform.localToWorldMatrix;

            // For bones
            boneMatrices[i] = smr.bones[0].localToWorldMatrix; // Simplified: assuming one bone per mesh
            newBones[i] = smr.bones[0];
        }

        // Create a new SkinnedMeshRenderer
        GameObject newObject = new GameObject("CombinedMesh");
        SkinnedMeshRenderer newRenderer = newObject.AddComponent<SkinnedMeshRenderer>();
        Mesh newMesh = new Mesh();
        newMesh.CombineMeshes(combineInstances, true, true);
        newRenderer.sharedMesh = newMesh;
        newRenderer.bones = newBones;

        // Assign materials (assuming all use the same material)
        newRenderer.material = meshRenderers[0].material;
    }
}
