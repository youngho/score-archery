using UnityEngine;
using UnityEditor;

public class RemoveMissingScripts
{
    [MenuItem("Tools/Remove Missing Scripts In Scene")]
    public static void RemoveInScene()
    {
        int totalRemoved = 0;
        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>(true);
        foreach (GameObject go in allObjects)
        {
            int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            if (count > 0)
            {
                Debug.Log($"Removed {count} missing scripts from {go.name}");
                totalRemoved += count;
            }
        }
        Debug.Log($"Total missing scripts removed from scene: {totalRemoved}");
    }
}