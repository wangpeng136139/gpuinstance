using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;



public class GPUInstancingHelper : MonoBehaviour
{
    [MenuItem("Tools/开启GPUInstance")]
    public static void OpenGPUInstance()
    {
        foreach (Object obj in Selection.objects)
        {
            if (obj is Material material)
            {
                material.enableInstancing = true;
                Debug.Log($"Enabled GPU Instancing for {material.name}");
            }
        }
    }

    // 批量启用场景中所有材质的 GPU Instancing
    [MenuItem("Tools/批量启用场景")]
    public static void EnableGPUInstancingForAllSceneMaterials()
    {
        Material[] allMaterials = Resources.FindObjectsOfTypeAll<Material>();
        int count = 0;

        foreach (Material mat in allMaterials)
        {
            if (!mat.enableInstancing)
            {
                mat.enableInstancing = true;
                count++;
            }
        }

        Debug.Log($"Enabled GPU Instancing for {count} materials");
    }

    private static void CreateMeshVertex()
    {

    }
}
