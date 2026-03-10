using UnityEngine;
using UnityEditor;
using System.Linq;

public class ContainerStacker : Editor
{
    [MenuItem("Tools/Stack Selected Containers (Y Axis)")]
    public static void StackContainers()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        if (selectedObjects.Length < 2)
        {
            Debug.LogWarning("컨테이너를 2개 이상 선택한 후 실행해주세요.");
            return;
        }

        // 현재 Y축(높이) 기준으로 오름차순 정렬하여 쌓을 순서를 파악합니다.
        var sortedObjects = selectedObjects.OrderBy(go => go.transform.position.y).ToArray();

        // 되돌리기(Ctrl+Z / Cmd+Z)를 위해 기록합니다.
        Undo.RecordObjects(sortedObjects.Select(go => go.transform).ToArray(), "Stack Containers");

        // 첫 번째(가장 아래쪽) 오브젝트의 꼭대기 Y 좌표
        float currentTargetY = GetMaxY(sortedObjects[0]);

        for (int i = 1; i < sortedObjects.Length; i++)
        {
            GameObject currentObj = sortedObjects[i];
            
            // 현재 오브젝트의 바닥 Y 좌표
            float minY = GetMinY(currentObj);
            
            // 이동해야 할 거리
            float offset = currentTargetY - minY;
            
            // Y축으로 오브젝트 이동
            currentObj.transform.position += new Vector3(0, offset, 0);
            
            // 이동한 후, 이 오브젝트의 새로운 꼭대기 Y 좌표를 다음 타겟으로 설정
            currentTargetY = GetMaxY(currentObj);
        }
        
        Debug.Log($"총 {selectedObjects.Length}개의 컨테이너를 성공적으로 쌓아 올렸습니다!");
    }

    private static float GetMinY(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return go.transform.position.y;

        float minY = float.MaxValue;
        foreach (var rend in renderers)
        {
            if (rend.bounds.min.y < minY)
            {
                minY = rend.bounds.min.y;
            }
        }
        return minY;
    }

    private static float GetMaxY(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return go.transform.position.y;

        float maxY = float.MinValue;
        foreach (var rend in renderers)
        {
            if (rend.bounds.max.y > maxY)
            {
                maxY = rend.bounds.max.y;
            }
        }
        return maxY;
    }
}