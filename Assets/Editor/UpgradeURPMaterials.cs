using UnityEngine;
using UnityEditor;

public class UpgradeURPMaterials
{
    [MenuItem("Tools/Upgrade SimpleLowPolyNature to URP")]
    public static void Upgrade()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/SimpleLowPolyNature/Materials" });
        Shader urpShader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (urpShader == null) urpShader = Shader.Find("Universal Render Pipeline/Lit");
        
        int count = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null && mat.shader.name == "Standard")
            {
                // Simple Lit is usually better for mobile/low poly if available
                mat.shader = urpShader;
                EditorUtility.SetDirty(mat);
                count++;
            }
        }

        // Also check if there are any other Standard materials in the project that need upgrading
        string[] allGuids = AssetDatabase.FindAssets("t:Material");
        foreach (string guid in allGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            // Skip packages
            if (path.StartsWith("Packages/")) continue;
            
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null && mat.shader.name == "Standard")
            {
                mat.shader = urpShader;
                EditorUtility.SetDirty(mat);
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Upgraded {count} materials to URP.");
    }
}