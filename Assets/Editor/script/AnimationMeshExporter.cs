using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class AnimationMeshExporter : EditorWindow
{
    /// <summary>
    /// 导出面板类
    /// </summary>
    [Serializable]
    public class GPUAnimationCreateClip
    {
        public AnimationClip aniclip;
        public int samplesPerSecond = 60;         //采样帧数
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
        // 步骤2：获取或创建窗口实例
        var window = GetWindow<AnimationMeshExporter>();
        window.titleContent = new GUIContent("Export Animation Meshes");
        window.minSize = new Vector2(800, 300); // 设置最小窗口尺寸
    }

    private void OnEnable()
    {
        serializedObject = new SerializedObject(this);
        clipsProp = serializedObject.FindProperty(nameof(clipList));
    }


    private void OnGUI()
    {
        serializedObject.Update();
        // 显示数组标题
        EditorGUILayout.LabelField("动画", EditorStyles.boldLabel);
        // 显示数组元素
        EditorGUILayout.PropertyField(clipsProp, new GUIContent("动画参数"), true);
        // 应用修改
        if (serializedObject.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(this); // 标记为需要保存
        }

        character = (GameObject)EditorGUILayout.ObjectField("角色模型", character, typeof(GameObject), true);
        animator = (Animator)EditorGUILayout.ObjectField("animator", animator, typeof(Animator), true);
        renderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("renderer", renderer, typeof(SkinnedMeshRenderer), true);

        if (GUILayout.Button("导出网格序列"))
        {
            // 执行操作逻辑
            EditorApplication.delayCall += ExportMeshes;
        }

        // 进度条显示
        EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), progressValue, "处理进度");
    }

    private void UpdateProgress()
    {
        progressValue += 0.01f;
        if (progressValue >= 1)
        {
            EditorApplication.update -= UpdateProgress;
        }
        Repaint(); // 强制刷新界面
    }

    private void ExportMeshes()
    {
        if (!character || clipList == null || clipList.Count <= 0) return;

        // 模拟进度更新
        progressValue = 0;
        EditorApplication.update += UpdateProgress;

        //创建Export文件夹
        if (!Directory.Exists(ExportPath))
        {
            Directory.CreateDirectory(ExportPath);
        }

        //创建角色文件夹
        string filePath = string.Format(FilePath, character.name);
        Directory.CreateDirectory(filePath);

        //创建Texture文件夹
        string texture = string.Format(TexturePath, character.name);
        Directory.CreateDirectory(texture);

        CreateMeshVertex();
        Texture2D vertex = CreateVertexTex();
        Texture2D normal = CreateNormalTex();
        CreateMaterial(vertex, normal);
        CreatePrefab();
        Debug.Log($"创建成功，文件路径:{filePath}");
    }

    //创建顶点
    private void CreateMeshVertex()
    {
        GPUAnimationData gpuAnimationData = ScriptableObject.CreateInstance<GPUAnimationData>();

        foreach (var clip in clipList)
        {
            GPUAnimationClip gPUAnimationClip = new GPUAnimationClip();
            gPUAnimationClip.aniclip = clip.aniclip;
            gPUAnimationClip.samplesPerSecond = clip.samplesPerSecond;
            gPUAnimationClip.StartFrame = gpuAnimationData.totalFrame; //当前动画的起始帧数

            float clipLength = clip.aniclip.length;
            int totalFrames = Mathf.FloorToInt(clipLength * clip.samplesPerSecond);

            for (int i = 0; i < totalFrames; i++)
            {
                float time = i / (float)clip.samplesPerSecond;
                // 采样动画到当前时间
                clip.aniclip.SampleAnimation(character, time);
                // 在编辑器运行时更新游戏逻辑但又不想打断编辑器的常规更新循环时
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                // 烘焙网格
                Mesh mesh = new Mesh();
                renderer.BakeMesh(mesh, true);
                Vector3[] vertexs = mesh.vertices;
                Vector3[] normals = mesh.normals;
                this.vertexs.Add(vertexs);
                this.normals.Add(normals);
            }
            gpuAnimationData.totalFrame += totalFrames;                             //计算总帧数
            gPUAnimationClip.EndFrame = gpuAnimationData.totalFrame;                //当前片段的结束帧数
            gpuAnimationData.AddClips(gPUAnimationClip);
        }

        string filePath = string.Format(clipsDataPath, character.name, $"{character.name}_data");
        // 保存为Asset文件
        AssetDatabase.CreateAsset(gpuAnimationData, filePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    //创建顶点贴图
    private Texture2D CreateVertexTex()
    {
        int height = vertexs.Count;
        int weight = vertexs[0].Length;
        // 创建Texture2D，使用RGBA32格式，关闭mipmap
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

        // 设置每个像素的颜色
        Color[] colors = new Color[height * weight];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < weight; x++)
            {
                Vector3 pos = vertexs[y][x];
                //将坐标压缩到0-1
                float r = (pos.x - xmin) / xrange;
                float g = (pos.y - ymin) / yrange;
                float b = (pos.z - zmin) / zrange;
                colors[y * weight + x] = new Color(r, g, b, 1f);
            }
        }
        tex.SetPixels(colors);
        tex.Apply();
        // 可选：设置过滤模式为点过滤（像素风格）
        tex.filterMode = FilterMode.Point;
        // 保存为PNG文件
        byte[] pngData = tex.EncodeToPNG();
        string filePath = string.Format(ImagePath, character.name, $"{character.name}_vertex");
        File.WriteAllBytes(filePath, pngData);
        // 刷新资源数据库并调整导入设置
        AssetDatabase.Refresh();
        TextureImporter importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
        if (importer != null)
        {

            importer.sRGBTexture = false;                 // 关闭 sRGB 颜色空间转换
            importer.filterMode = FilterMode.Point;     // 设置过滤模式为Point
            importer.mipmapEnabled = false;             // 关闭Mipmap
            importer.textureCompression = TextureImporterCompression.Uncompressed; //禁用压缩
            importer.maxTextureSize = 8192;
            importer.npotScale = TextureImporterNPOTScale.None; //禁止对非二次幂（NPOT）纹理进行自动缩放
            importer.SaveAndReimport();                         // 应用更改
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
    /// 创建法线贴图
    /// </summary>
    private Texture2D CreateNormalTex()
    {
        int height = normals.Count;
        int weight = normals[0].Length;
        // 创建Texture2D，使用RGBA32格式，关闭mipmap
        Texture2D tex = new Texture2D(weight, height, TextureFormat.RGBA32, false);

        // 设置每个像素的颜色
        Color[] colors = new Color[height * weight];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < weight; x++)
            {
                Vector3 normal = normals[y][x];
                // 示例：左上角红色，右下角蓝色，中间渐变
                float max = Mathf.Max(normal.x, normal.y, normal.z);
                float r = normal.x / max;
                float g = normal.y / max;
                float b = normal.z / max;
                colors[y * weight + x] = new Color(r, g, b, 1f);
            }
        }
        tex.SetPixels(colors);
        tex.Apply();
        // 可选：设置过滤模式为点过滤（像素风格）
        tex.filterMode = FilterMode.Point;
        // 保存为PNG文件
        byte[] pngData = tex.EncodeToPNG();
        string filePath = string.Format(ImagePath, character.name, $"{character.name}_normal");
        File.WriteAllBytes(filePath, pngData);
        // 刷新资源数据库并调整导入设置
        AssetDatabase.Refresh();

        TextureImporter importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
        if (importer != null)
        {
            // 关闭 sRGB 颜色空间转换
            importer.sRGBTexture = false;
            importer.filterMode = FilterMode.Point; // 设置过滤模式为Point
            importer.mipmapEnabled = false; // 关闭Mipmap
            importer.textureCompression = TextureImporterCompression.Uncompressed; // 可选：禁用压缩
            importer.npotScale = TextureImporterNPOTScale.None; //禁止对非二次幂（NPOT）纹理进行自动缩放
            importer.maxTextureSize = 8192;
            importer.SaveAndReimport(); // 应用更改
        }

        tex = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
        return tex;
    }

    private void CreatePrefab()
    {
        GameObject newPlayer = GameObject.Instantiate(character);

        newPlayer.name = character.name + "(GPUInstance)";
        GPUAnimation gpu = newPlayer.AddComponent<GPUAnimation>();
        //加载数据文件
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

        // 3. 将GameObject保存为预制体
        string filePath = string.Format(PrefabPath, character.name, newPlayer.name);
        PrefabUtility.SaveAsPrefabAsset(newPlayer, filePath);
        DestroyImmediate(newPlayer);
    }

    private void CreateMaterial(Texture posTexture, Texture normalTexture)
    {
        // 1. 创建新材质
        Shader standardShader = Shader.Find("Custom/InstancedShader");
        Material newMaterial = new Material(standardShader)
        {
            name = $"{character.name}_mat",
        };
        //设置主纹理
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
        //设置顶点纹理
        newMaterial.SetTexture(MAT_VAR_POSTEXTURE, posTexture);
        //设置法线纹理
        newMaterial.SetTexture(MAT_VAR_NORMALTEXTURE, normalTexture);

        newMaterial.enableInstancing = true;

        string filePath = string.Format(MaterialPath, character.name, $"{character.name}_mat");
        // 保存材质
        AssetDatabase.CreateAsset(newMaterial, filePath);
        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();
    }

}