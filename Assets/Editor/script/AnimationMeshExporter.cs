using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class AnimationMeshExporter : EditorWindow
{
    /// <summary>
    /// ���������
    /// </summary>
    [Serializable]
    public class GPUAnimationCreateClip
    {
        public AnimationClip aniclip;
        public int samplesPerSecond = 60;         //����֡��
    }

    private float progressValue = 0;

    public GameObject character;
    public SkinnedMeshRenderer renderer;
    public Animator animator;

    public List<GPUAnimationCreateClip> clipList = new List<GPUAnimationCreateClip>();
    private SerializedObject serializedObject;
    private SerializedProperty clipsProp;

    private List<Vector3[]> vertexs = new List<Vector3[]>();
    private List<Vector3[]> normals = new List<Vector3[]>();

    private Vector3 minPos;
    private Vector3 range;

    private static string ExportPath = "Assets/Plug/GPUInstance/Export";
    private static string FilePath = $"{ExportPath}/{{0}}";
    private static string clipsDataPath = $"{ExportPath}/{{0}}/{{1}}.asset";
    private static string TexturePath = $"{ExportPath}/{{0}}/Texture";
    private static string ImagePath = $"{ExportPath}/{{0}}/Texture/{{1}}.png";
    private static string MaterialPath = $"{ExportPath}/{{0}}/{{1}}.mat";
    private static string PrefabPath = $"{ExportPath}/{{0}}/{{1}}.prefab";

    private const string MAT_VAR_VERTEXCOUNT = "_VertexCount";
    private const string MAT_VAR_MAXMEASURE = "_Range";
    private const string MAT_VAR_MINPOS = "_MinPos";
    private const string MAT_VAR_MAINTEXTURE = "_MainTex";
    private const string MAT_VAR_POSTEXTURE = "_PosTex";
    private const string MAT_VAR_NORMALTEXTURE = "_NormalTex";

    [MenuItem("Tools/Export Animation Meshes")]
    private static void Init()
    {
        // ����2����ȡ�򴴽�����ʵ��
        var window = GetWindow<AnimationMeshExporter>();
        window.titleContent = new GUIContent("Export Animation Meshes");
        window.minSize = new Vector2(800, 300); // ������С���ڳߴ�
    }

    private void OnEnable()
    {
        serializedObject = new SerializedObject(this);
        clipsProp = serializedObject.FindProperty(nameof(clipList));
    }


    private void OnGUI()
    {
        serializedObject.Update();
        // ��ʾ�������
        EditorGUILayout.LabelField("����", EditorStyles.boldLabel);
        // ��ʾ����Ԫ��
        EditorGUILayout.PropertyField(clipsProp, new GUIContent("��������"), true);
        // Ӧ���޸�
        if (serializedObject.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(this); // ���Ϊ��Ҫ����
        }

        character = (GameObject)EditorGUILayout.ObjectField("��ɫģ��", character, typeof(GameObject), true);
        animator = (Animator)EditorGUILayout.ObjectField("animator", animator, typeof(Animator), true);
        renderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("renderer", renderer, typeof(SkinnedMeshRenderer), true);

        if (GUILayout.Button("������������"))
        {
            // ִ�в����߼�
            EditorApplication.delayCall += ExportMeshes;
        }

        // ��������ʾ
        EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), progressValue, "�������");
    }

    private void UpdateProgress()
    {
        progressValue += 0.01f;
        if (progressValue >= 1)
        {
            EditorApplication.update -= UpdateProgress;
        }
        Repaint(); // ǿ��ˢ�½���
    }

    private void ExportMeshes()
    {
        if (!character || clipList == null || clipList.Count <= 0) return;

        // ģ����ȸ���
        progressValue = 0;
        EditorApplication.update += UpdateProgress;

        //����Export�ļ���
        if (!Directory.Exists(ExportPath))
        {
            Directory.CreateDirectory(ExportPath);
        }

        //������ɫ�ļ���
        string filePath = string.Format(FilePath, character.name);
        Directory.CreateDirectory(filePath);

        //����Texture�ļ���
        string texture = string.Format(TexturePath, character.name);
        Directory.CreateDirectory(texture);

        CreateMeshVertex();
        Texture2D vertex = CreateVertexTex();
        Texture2D normal = CreateNormalTex();
        CreateMaterial(vertex, normal);
        CreatePrefab();
        Debug.Log($"�����ɹ����ļ�·��:{filePath}");
    }

    //��������
    private void CreateMeshVertex()
    {
        GPUAnimationData gpuAnimationData = ScriptableObject.CreateInstance<GPUAnimationData>();

        foreach (var clip in clipList)
        {
            GPUAnimationClip gPUAnimationClip = new GPUAnimationClip();
            gPUAnimationClip.aniclip = clip.aniclip;
            gPUAnimationClip.samplesPerSecond = clip.samplesPerSecond;
            gPUAnimationClip.StartFrame = gpuAnimationData.totalFrame; //��ǰ��������ʼ֡��

            float clipLength = clip.aniclip.length;
            int totalFrames = Mathf.FloorToInt(clipLength * clip.samplesPerSecond);

            for (int i = 0; i < totalFrames; i++)
            {
                float time = i / (float)clip.samplesPerSecond;
                // ������������ǰʱ��
                clip.aniclip.SampleAnimation(character, time);
                // �ڱ༭������ʱ������Ϸ�߼����ֲ����ϱ༭���ĳ������ѭ��ʱ
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                // �決����
                Mesh mesh = new Mesh();
                renderer.BakeMesh(mesh, true);
                Vector3[] vertexs = mesh.vertices;
                Vector3[] normals = mesh.normals;
                this.vertexs.Add(vertexs);
                this.normals.Add(normals);
            }
            gpuAnimationData.totalFrame += totalFrames;                             //������֡��
            gPUAnimationClip.EndFrame = gpuAnimationData.totalFrame;                //��ǰƬ�εĽ���֡��
            gpuAnimationData.AddClips(gPUAnimationClip);
        }

        string filePath = string.Format(clipsDataPath, character.name, $"{character.name}_data");
        // ����ΪAsset�ļ�
        AssetDatabase.CreateAsset(gpuAnimationData, filePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    //����������ͼ
    private Texture2D CreateVertexTex()
    {
        int height = vertexs.Count;
        int weight = vertexs[0].Length;
        // ����Texture2D��ʹ��RGBA32��ʽ���ر�mipmap
        Texture2D tex = new Texture2D(weight, height, TextureFormat.RGBA32, false);

        float xmin, ymin, zmin;
        float xmax, ymax, zmax;
        (xmin, ymin, zmin) = FindExtremum(true);
        minPos = new Vector3(xmin, ymin, zmin);
        (xmax, ymax, zmax) = FindExtremum(false);
        float xrange = xmax - xmin;
        float yrange = ymax - ymin;
        float zrange = zmax - zmin;
        range = new Vector3(xrange, yrange, zrange);

        // ����ÿ�����ص���ɫ
        Color[] colors = new Color[height * weight];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < weight; x++)
            {
                Vector3 pos = vertexs[y][x];
                //������ѹ����0-1
                float r = (pos.x - xmin) / xrange;
                float g = (pos.y - ymin) / yrange;
                float b = (pos.z - zmin) / zrange;
                colors[y * weight + x] = new Color(r, g, b, 1f);
            }
        }
        tex.SetPixels(colors);
        tex.Apply();
        // ��ѡ�����ù���ģʽΪ����ˣ����ط��
        tex.filterMode = FilterMode.Point;
        // ����ΪPNG�ļ�
        byte[] pngData = tex.EncodeToPNG();
        string filePath = string.Format(ImagePath, character.name, $"{character.name}_vertex");
        File.WriteAllBytes(filePath, pngData);
        // ˢ����Դ���ݿⲢ������������
        AssetDatabase.Refresh();
        TextureImporter importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
        if (importer != null)
        {

            importer.sRGBTexture = false;                 // �ر� sRGB ��ɫ�ռ�ת��
            importer.filterMode = FilterMode.Point;     // ���ù���ģʽΪPoint
            importer.mipmapEnabled = false;             // �ر�Mipmap
            importer.textureCompression = TextureImporterCompression.Uncompressed; //����ѹ��
            importer.maxTextureSize = 8192;
            importer.npotScale = TextureImporterNPOTScale.None; //��ֹ�ԷǶ����ݣ�NPOT����������Զ�����
            importer.SaveAndReimport();                         // Ӧ�ø���
        }
        tex = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);

        return tex;
    }

    private (float, float, float) FindExtremum(bool findMin)
    {
        int width = vertexs.Count;
        int height = vertexs[0].Length;

        float initial = findMin ? float.MaxValue : float.MinValue;
        float x = initial, y = initial, z = initial;

        Func<float, float, float> compare = findMin
            ? (a, b) => MathF.Min(a, b)
            : (a, b) => MathF.Max(a, b);

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                Vector3 pos = vertexs[j][i];
                x = compare(x, pos.x);
                y = compare(y, pos.y);
                z = compare(z, pos.z);
            }
        }
        return (x, y, z);
    }


    private (float, float, float) FindMinValue() => FindExtremum(true);
    private (float, float, float) FindMaxValue() => FindExtremum(false);

    /// <summary>
    /// ����������ͼ
    /// </summary>
    private Texture2D CreateNormalTex()
    {
        int height = normals.Count;
        int weight = normals[0].Length;
        // ����Texture2D��ʹ��RGBA32��ʽ���ر�mipmap
        Texture2D tex = new Texture2D(weight, height, TextureFormat.RGBA32, false);

        // ����ÿ�����ص���ɫ
        Color[] colors = new Color[height * weight];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < weight; x++)
            {
                Vector3 normal = normals[y][x];
                // ʾ�������ϽǺ�ɫ�����½���ɫ���м佥��
                float max = Mathf.Max(normal.x, normal.y, normal.z);
                float r = normal.x / max;
                float g = normal.y / max;
                float b = normal.z / max;
                colors[y * weight + x] = new Color(r, g, b, 1f);
            }
        }
        tex.SetPixels(colors);
        tex.Apply();
        // ��ѡ�����ù���ģʽΪ����ˣ����ط��
        tex.filterMode = FilterMode.Point;
        // ����ΪPNG�ļ�
        byte[] pngData = tex.EncodeToPNG();
        string filePath = string.Format(ImagePath, character.name, $"{character.name}_normal");
        File.WriteAllBytes(filePath, pngData);
        // ˢ����Դ���ݿⲢ������������
        AssetDatabase.Refresh();

        TextureImporter importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
        if (importer != null)
        {
            // �ر� sRGB ��ɫ�ռ�ת��
            importer.sRGBTexture = false;
            importer.filterMode = FilterMode.Point; // ���ù���ģʽΪPoint
            importer.mipmapEnabled = false; // �ر�Mipmap
            importer.textureCompression = TextureImporterCompression.Uncompressed; // ��ѡ������ѹ��
            importer.npotScale = TextureImporterNPOTScale.None; //��ֹ�ԷǶ����ݣ�NPOT����������Զ�����
            importer.maxTextureSize = 8192;
            importer.SaveAndReimport(); // Ӧ�ø���
        }

        tex = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
        return tex;
    }

    private void CreatePrefab()
    {
        GameObject newPlayer = GameObject.Instantiate(character);

        newPlayer.name = character.name + "(GPUInstance)";
        GPUAnimation gpu = newPlayer.AddComponent<GPUAnimation>();
        //���������ļ�
        string GpuAnimationDataPath = string.Format(clipsDataPath, character.name, $"{character.name}_data");
        gpu.data = AssetDatabase.LoadAssetAtPath<GPUAnimationData>(GpuAnimationDataPath);
        SkinnedMeshRenderer[] renderers = newPlayer.GetComponentsInChildren<SkinnedMeshRenderer>();

        for (int i = 0; i < renderers.Length; i++)
        {
            GameObject obj = renderers[i].gameObject;

            MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
            meshFilter.mesh = renderers[i].sharedMesh;

            MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
            string newMaterialPath = string.Format(MaterialPath, character.name, $"{character.name}_mat");
            Material newMaterial = AssetDatabase.LoadAssetAtPath<Material>(newMaterialPath);
            meshRenderer.sharedMaterial = newMaterial;
            gpu.mesh = meshRenderer;

            DestroyImmediate(renderers[i]);
        }

        Animator a = newPlayer.GetComponentInChildren<Animator>();
        DestroyImmediate(a);

        // 3. ��GameObject����ΪԤ����
        string filePath = string.Format(PrefabPath, character.name, newPlayer.name);
        PrefabUtility.SaveAsPrefabAsset(newPlayer, filePath);
        DestroyImmediate(newPlayer);
    }

    private void CreateMaterial(Texture posTexture, Texture normalTexture)
    {
        // 1. �����²���
        Shader standardShader = Shader.Find("Custom/InstancedShader");
        Material newMaterial = new Material(standardShader)
        {
            name = $"{character.name}_mat",
        };
        //����������
        if (renderer.sharedMaterial != null)
        {
            Texture mainTex = renderer.sharedMaterial.GetTexture(MAT_VAR_MAINTEXTURE);
            if (mainTex != null)
            {
                newMaterial.SetTexture(MAT_VAR_MAINTEXTURE, mainTex);
            }
        }
        newMaterial.SetInt(MAT_VAR_VERTEXCOUNT, renderer.sharedMesh.vertexCount);
        newMaterial.SetVector(MAT_VAR_MINPOS, minPos);
        newMaterial.SetVector(MAT_VAR_MAXMEASURE, range);
        //���ö�������
        newMaterial.SetTexture(MAT_VAR_POSTEXTURE, posTexture);
        //���÷�������
        newMaterial.SetTexture(MAT_VAR_NORMALTEXTURE, normalTexture);

        newMaterial.enableInstancing = true;

        string filePath = string.Format(MaterialPath, character.name, $"{character.name}_mat");
        // �������
        AssetDatabase.CreateAsset(newMaterial, filePath);
        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();
    }

}