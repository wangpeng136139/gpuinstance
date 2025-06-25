using UnityEngine;

public class MeshVisibilityController : MonoBehaviour
{
    public Camera mainCamera; // 主摄像机
    public SkinnedMeshRenderer combinedMeshRenderer; // 合并后的SkinnedMeshRenderer

    void Update()
    {
        CheckVisibility();
    }

    void CheckVisibility()
    {
        if (mainCamera == null || combinedMeshRenderer == null)
            return;

        // 获取摄像机的视锥平面
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(mainCamera);
            
        // 获取合并网格的边界框
        Bounds bounds = combinedMeshRenderer.bounds;

        // 检查边界框是否在视锥内
        bool isVisible = GeometryUtility.TestPlanesAABB(planes, bounds);

        // 根据可见性设置合并网格的渲染器启用状态
        combinedMeshRenderer.enabled = isVisible;
    }
}
