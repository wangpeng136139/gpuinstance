using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;



public class GPUInstancingHelper : MonoBehaviour
{
    [MenuItem("Tools/����GPUInstance")]
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

    // �������ó��������в��ʵ� GPU Instancing
    [MenuItem("Tools/�������ó���")]
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
