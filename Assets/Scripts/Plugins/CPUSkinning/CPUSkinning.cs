using UnityEngine;


public class CPUSkinning : MonoBehaviour
{
    [Header("骨骼设置")]
    public Transform rootBone;
    public Transform[] bones;
    public Material skinMaterial;

    [Header("性能优化")]
    public bool updateEveryFrame = true;
    public float updateInterval = 0.033f; // 约30FPS

    [Header("Animator设置")]
    public bool useAnimator = true;
    public Animator animator;
    public string animationName;
    public float animationSpeed = 1.0f;

    private Mesh originalMesh;
    private Mesh skinnedMesh;
    private Matrix4x4[] bindPoses;
    private Matrix4x4[] boneMatrices;
    private Vector3[] vertices;
    private Vector3[] skinnedVertices;
    private Vector3[] normals;
    private Vector3[] skinnedNormals;
    private BoneWeight[] boneWeights;

    private float lastUpdateTime = 0;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private int animationLayerIndex = 0;

    void Start()
    {
        InitializeSkinning();
        InitializeAnimator();
    }

    void InitializeSkinning()
    {
        // 获取组件引用
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        animator = GetComponent<Animator>();

        // 复制原始网格数据
        originalMesh = meshFilter.sharedMesh;
        skinnedMesh = new Mesh();
        skinnedMesh.name = "SkinnedMesh";
        skinnedMesh.indexFormat = originalMesh.indexFormat;
        meshFilter.mesh = skinnedMesh;

        // 设置材质
        meshRenderer.material = skinMaterial;

        // 初始化骨骼数据
        if (bones == null || bones.Length == 0)
        {
            CollectBones();
        }

        // 复制网格数据
        vertices = originalMesh.vertices;
        skinnedVertices = new Vector3[vertices.Length];
        normals = originalMesh.normals;
        skinnedNormals = new Vector3[normals.Length];
        boneWeights = originalMesh.boneWeights;
        bindPoses = originalMesh.bindposes;
        boneMatrices = new Matrix4x4[bones.Length];

        // 执行首次蒙皮计算
        UpdateBoneMatrices();
        UpdateSkinnedMesh();
    }

    void InitializeAnimator()
    {
        if (!useAnimator || animator == null) return;

        // 设置动画参数
        animator.speed = animationSpeed;

        // 如果指定了动画名称，播放该动画
        if (!string.IsNullOrEmpty(animationName))
        {
            animator.Play(animationName, animationLayerIndex);
        }
    }

    void CollectBones()
    {
        // 如果没有指定骨骼，自动从Animator中收集
        if (rootBone == null && animator != null && animator.avatar != null && animator.avatar.isValid)
        {
            // 从Animator的HumanDescription中获取骨骼
            HumanDescription humanDescription = animator.avatar.humanDescription;
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);

            if (hips != null)
            {
                rootBone = hips;
                bones = rootBone.GetComponentsInChildren<Transform>();
            }
            else
            {
                // 如果不是人形动画，使用通用方法
                rootBone = animator.transform;
                bones = rootBone.GetComponentsInChildren<Transform>();
            }
        }
        else if (rootBone != null)
        {
            // 传统方式收集骨骼
            bones = rootBone.GetComponentsInChildren<Transform>();
        }
        else
        {
            Debug.LogError("Root bone is not assigned and cannot be automatically collected from Animator!");
        }
    }

    void Update()
    {
        if (!updateEveryFrame) return;

        // 控制更新频率
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateSkinning();
            lastUpdateTime = Time.time;
        }
    }

    public void UpdateSkinning()
    {
        // 如果使用Animator，确保它已经更新
        if (useAnimator && animator != null)
        {
            // 手动更新Animator
            animator.Update(0);
        }

        UpdateBoneMatrices();
        UpdateSkinnedMesh();
    }

    void UpdateBoneMatrices()
    {
        // 计算每根骨骼的世界变换矩阵
        for (int i = 0; i < bones.Length; i++)
        {
            if (bones[i] != null)
            {
                boneMatrices[i] = bones[i].localToWorldMatrix * bindPoses[i];
            }
        }
    }

    void UpdateSkinnedMesh()
    {
        // 对每个顶点应用骨骼蒙皮计算
        for (int i = 0; i < vertices.Length; i++)
        {
            BoneWeight weight = boneWeights[i];
            Vector3 vertex = vertices[i];
            Vector3 normal = normals[i];

            // 应用骨骼变换
            Vector3 skinnedVertex = ApplyBoneTransform(vertex, weight, boneMatrices);
            Vector3 skinnedNormal = ApplyBoneTransform(normal, weight, boneMatrices, true);

            skinnedVertices[i] = skinnedVertex;
            skinnedNormals[i] = skinnedNormal;
        }

        // 更新蒙皮后的网格数据
        skinnedMesh.vertices = skinnedVertices;
        skinnedMesh.normals = skinnedNormals;
        skinnedMesh.triangles = originalMesh.triangles;
        skinnedMesh.uv = originalMesh.uv;
        skinnedMesh.RecalculateBounds();
    }

    Vector3 ApplyBoneTransform(Vector3 point, BoneWeight weight, Matrix4x4[] boneMatrices, bool isNormal = false)
    {
        // 计算顶点受多根骨骼影响的最终变换
        Vector3 finalPoint = Vector3.zero;

        if (weight.weight0 > 0)
        {
            Matrix4x4 matrix = boneMatrices[weight.boneIndex0];
            finalPoint += matrix.MultiplyPoint3x4(point) * weight.weight0;
        }

        if (weight.weight1 > 0)
        {
            Matrix4x4 matrix = boneMatrices[weight.boneIndex1];
            finalPoint += matrix.MultiplyPoint3x4(point) * weight.weight1;
        }

        if (weight.weight2 > 0)
        {
            Matrix4x4 matrix = boneMatrices[weight.boneIndex2];
            finalPoint += matrix.MultiplyPoint3x4(point) * weight.weight2;
        }

        if (weight.weight3 > 0)
        {
            Matrix4x4 matrix = boneMatrices[weight.boneIndex3];
            finalPoint += matrix.MultiplyPoint3x4(point) * weight.weight3;
        }

        return finalPoint;
    }
}