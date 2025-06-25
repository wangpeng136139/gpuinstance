using UnityEngine;


public class CPUSkinning : MonoBehaviour
{
    [Header("��������")]
    public Transform rootBone;
    public Transform[] bones;
    public Material skinMaterial;

    [Header("�����Ż�")]
    public bool updateEveryFrame = true;
    public float updateInterval = 0.033f; // Լ30FPS

    [Header("Animator����")]
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
        // ��ȡ�������
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        animator = GetComponent<Animator>();

        // ����ԭʼ��������
        originalMesh = meshFilter.sharedMesh;
        skinnedMesh = new Mesh();
        skinnedMesh.name = "SkinnedMesh";
        skinnedMesh.indexFormat = originalMesh.indexFormat;
        meshFilter.mesh = skinnedMesh;

        // ���ò���
        meshRenderer.material = skinMaterial;

        // ��ʼ����������
        if (bones == null || bones.Length == 0)
        {
            CollectBones();
        }

        // ������������
        vertices = originalMesh.vertices;
        skinnedVertices = new Vector3[vertices.Length];
        normals = originalMesh.normals;
        skinnedNormals = new Vector3[normals.Length];
        boneWeights = originalMesh.boneWeights;
        bindPoses = originalMesh.bindposes;
        boneMatrices = new Matrix4x4[bones.Length];

        // ִ���״���Ƥ����
        UpdateBoneMatrices();
        UpdateSkinnedMesh();
    }

    void InitializeAnimator()
    {
        if (!useAnimator || animator == null) return;

        // ���ö�������
        animator.speed = animationSpeed;

        // ���ָ���˶������ƣ����Ÿö���
        if (!string.IsNullOrEmpty(animationName))
        {
            animator.Play(animationName, animationLayerIndex);
        }
    }

    void CollectBones()
    {
        // ���û��ָ���������Զ���Animator���ռ�
        if (rootBone == null && animator != null && animator.avatar != null && animator.avatar.isValid)
        {
            // ��Animator��HumanDescription�л�ȡ����
            HumanDescription humanDescription = animator.avatar.humanDescription;
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);

            if (hips != null)
            {
                rootBone = hips;
                bones = rootBone.GetComponentsInChildren<Transform>();
            }
            else
            {
                // ����������ζ�����ʹ��ͨ�÷���
                rootBone = animator.transform;
                bones = rootBone.GetComponentsInChildren<Transform>();
            }
        }
        else if (rootBone != null)
        {
            // ��ͳ��ʽ�ռ�����
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

        // ���Ƹ���Ƶ��
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateSkinning();
            lastUpdateTime = Time.time;
        }
    }

    public void UpdateSkinning()
    {
        // ���ʹ��Animator��ȷ�����Ѿ�����
        if (useAnimator && animator != null)
        {
            // �ֶ�����Animator
            animator.Update(0);
        }

        UpdateBoneMatrices();
        UpdateSkinnedMesh();
    }

    void UpdateBoneMatrices()
    {
        // ����ÿ������������任����
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
        // ��ÿ������Ӧ�ù�����Ƥ����
        for (int i = 0; i < vertices.Length; i++)
        {
            BoneWeight weight = boneWeights[i];
            Vector3 vertex = vertices[i];
            Vector3 normal = normals[i];

            // Ӧ�ù����任
            Vector3 skinnedVertex = ApplyBoneTransform(vertex, weight, boneMatrices);
            Vector3 skinnedNormal = ApplyBoneTransform(normal, weight, boneMatrices, true);

            skinnedVertices[i] = skinnedVertex;
            skinnedNormals[i] = skinnedNormal;
        }

        // ������Ƥ�����������
        skinnedMesh.vertices = skinnedVertices;
        skinnedMesh.normals = skinnedNormals;
        skinnedMesh.triangles = originalMesh.triangles;
        skinnedMesh.uv = originalMesh.uv;
        skinnedMesh.RecalculateBounds();
    }

    Vector3 ApplyBoneTransform(Vector3 point, BoneWeight weight, Matrix4x4[] boneMatrices, bool isNormal = false)
    {
        // ���㶥���ܶ������Ӱ������ձ任
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