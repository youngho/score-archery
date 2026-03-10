using UnityEngine;
using UnityEditor;

public class ContainerUniformScaler : EditorWindow
{
    private GameObject referenceContainer;

    [MenuItem("Tools/Make Selected Containers Uniform Size v2")]
    public static void ShowWindow()
    {
        GetWindow<ContainerUniformScaler>("Uniform Container Scaler v2");
    }

    private void OnGUI()
    {
        GUILayout.Label("Uniform Container Scaler (회전 오차 수정 버전)", EditorStyles.boldLabel);
        
        EditorGUILayout.HelpBox(
            "1. 기준이 될 컨테이너를 Reference Container에 드래그 앤 드롭하세요.\n" +
            "2. 크기를 맞출 나머지 컨테이너들을 Hierarchy에서 선택하세요.\n" +
            "3. 아래 버튼을 누르면 컨테이너가 아무리 제각각 회전해 있어도 모양 찌그러짐 없이 똑바르게 동일 크기로 맞춰집니다.", 
            MessageType.Info);

        referenceContainer = (GameObject)EditorGUILayout.ObjectField("Reference Container", referenceContainer, typeof(GameObject), true);

        if (GUILayout.Button("Apply Uniform Scaling (Fix Rotation)"))
        {
            ApplyUniformScale();
        }
    }

    private void ApplyUniformScale()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        
        if (referenceContainer == null)
        {
            Debug.LogError("기준이 될 Reference Container를 할당해주세요.");
            return;
        }

        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("크기를 변경할 컨테이너를 Hierarchy에서 선택해주세요.");
            return;
        }

        // 1. 기준 컨테이너의 회전 오차가 제거된 실제 크기 측정
        Vector3 targetSize = GetTrueSize(referenceContainer);
        if (targetSize == Vector3.zero)
        {
            Debug.LogError("Reference Container의 크기를 계산할 수 없습니다.");
            return;
        }

        int processedCount = 0;

        foreach (var obj in selectedObjects)
        {
            if (obj == referenceContainer) continue;

            Undo.RecordObject(obj.transform, "Uniform Container Scaling");

            // 2. 대상의 회전 오차가 제거된 실제 크기 측정
            Vector3 currentSize = GetTrueSize(obj);
            if (currentSize == Vector3.zero) continue;

            // 3. 비율 계산: 각 축(X,Y,Z)별로 현재 크기가 목표 크기가 되기 위한 배율 계산
            float factorX = currentSize.x > 0.001f ? (targetSize.x / currentSize.x) : 1f;
            float factorY = currentSize.y > 0.001f ? (targetSize.y / currentSize.y) : 1f;
            float factorZ = currentSize.z > 0.001f ? (targetSize.z / currentSize.z) : 1f;

            // 4. 스케일 적용
            Vector3 currentScale = obj.transform.localScale;
            obj.transform.localScale = new Vector3(
                currentScale.x * factorX,
                currentScale.y * factorY,
                currentScale.z * factorZ
            );
            
            processedCount++;
        }

        Debug.Log($"[Uniform Scaler v2] {processedCount}개의 컨테이너 크기를 {referenceContainer.name}과 동일하게 조정했습니다. (목표 크기 X:{targetSize.x:F2}, Y:{targetSize.y:F2}, Z:{targetSize.z:F2})");
    }

    // 오브젝트의 회전을 일시적으로 초기화(Identity)한 상태에서의 실제 3D 크기(Bounds)를 반환합니다.
    private Vector3 GetTrueSize(GameObject go)
    {
        // 최상위 루트의 현재 회전 저장 후 초기화
        Quaternion originalRot = go.transform.rotation;
        go.transform.rotation = Quaternion.identity;

        // 회전이 없는 상태에서 모든 하위 렌더러의 바운딩 박스를 계산
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            go.transform.rotation = originalRot;
            return Vector3.zero;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        // 원래 회전으로 원복
        go.transform.rotation = originalRot;
        
        return bounds.size;
    }
}