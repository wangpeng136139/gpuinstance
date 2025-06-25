using UnityEngine;

public class MeshVisibilityController : MonoBehaviour
{
    public Camera mainCamera; // �������
    public SkinnedMeshRenderer combinedMeshRenderer; // �ϲ����SkinnedMeshRenderer

    void Update()
    {
        CheckVisibility();
    }

    void CheckVisibility()
    {
        if (mainCamera == null || combinedMeshRenderer == null)
            return;

        // ��ȡ���������׶ƽ��
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(mainCamera);
            
        // ��ȡ�ϲ�����ı߽��
        Bounds bounds = combinedMeshRenderer.bounds;

        // ���߽���Ƿ�����׶��
        bool isVisible = GeometryUtility.TestPlanesAABB(planes, bounds);

        // ���ݿɼ������úϲ��������Ⱦ������״̬
        combinedMeshRenderer.enabled = isVisible;
    }
}
