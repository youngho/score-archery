using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class ContainerScaleNormalizer : EditorWindow
{
    [MenuItem("Tools/Normalize Container Scales to Container7")]
    public static void NormalizeContainerScales()
    {
        // Find container7
        GameObject container7 = GameObject.Find("container7");
        if (container7 == null)
        {
            Debug.LogError("container7 not found in scene!");
            return;
        }

        Bounds container7Bounds = GetWorldBounds(container7);
        if (container7Bounds.size == Vector3.zero)
        {
            Debug.LogError("container7 has no renderers / bounds found!");
            return;
        }

        Vector3 c7Size = container7Bounds.size;
        float c7LongestAxis = Mathf.Max(c7Size.x, c7Size.y, c7Size.z);
        Debug.Log($"[ContainerScaleNormalizer] container7 world bounds size: {c7Size}, longest axis: {c7LongestAxis}");

        // Target: only "container"
        string targetName = "container";
        GameObject go = GameObject.Find(targetName);
        if (go == null)
        {
            Debug.LogError($"[ContainerScaleNormalizer] '{targetName}' not found in scene!");
            return;
        }

        Bounds bounds = GetWorldBounds(go);
        if (bounds.size == Vector3.zero)
        {
            Debug.LogError($"[ContainerScaleNormalizer] '{targetName}' has no renderers / bounds found!");
            return;
        }

        Vector3 size = bounds.size;
        float longestAxis = Mathf.Max(size.x, size.y, size.z);
        float ratio = c7LongestAxis / longestAxis;

        Vector3 currentScale = go.transform.localScale;
        Vector3 newScale = currentScale * ratio;

        Undo.RecordObject(go.transform, $"Normalize {targetName} Scale");
        go.transform.localScale = newScale;

        Debug.Log($"[ContainerScaleNormalizer] {targetName}: worldSize={size}, longestAxis={longestAxis:F4}, ratio={ratio:F4}, scale {currentScale} -> {newScale}");

        EditorSceneManager.MarkAllScenesDirty();
        Debug.Log("[ContainerScaleNormalizer] Done!");
    }

    private static Bounds GetWorldBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(go.transform.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }
}
