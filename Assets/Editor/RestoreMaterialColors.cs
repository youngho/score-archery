using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class RestoreMaterialColors
{
    [MenuItem("Tools/Restore Material Colors")]
    public static void Restore()
    {
        // Original colors from the Standard shader _Color property (captured before conversion)
        var colorMap = new Dictionary<string, Color>
        {
            { "TreeGreen1Mat", new Color(0.302f, 0.594f, 0.154f, 1f) },
            { "TreeGreen2Mat", new Color(0.302f, 0.594f, 0.154f, 1f) },
            { "TreeOrangeMat", new Color(0.887f, 0.488f, 0.093f, 1f) },
            { "TreePinkMat", new Color(0.943f, 0.574f, 0.734f, 1f) },
            { "TreeRedMat", new Color(0.811f, 0.172f, 0.149f, 1f) },
            { "TreeYellowMat", new Color(0.943f, 0.797f, 0.185f, 1f) },
            { "PlantMat", new Color(0.302f, 0.594f, 0.154f, 1f) },
            { "Flower1Mat", new Color(0.943f, 0.213f, 0.325f, 1f) },
            { "Flower2Mat", new Color(0.811f, 0.614f, 0.943f, 1f) },
            { "Flower3Mat", new Color(0.943f, 0.797f, 0.185f, 1f) },
            { "Flower4Mat", new Color(0.943f, 0.574f, 0.734f, 1f) },
            { "Mushroom1Mat", new Color(0.811f, 0.172f, 0.149f, 1f) },
            { "Mushroom2Mat", new Color(0.943f, 0.797f, 0.185f, 1f) },
            { "Mushroom3Mat", new Color(0.811f, 0.614f, 0.943f, 1f) },
            { "RockMat", new Color(0.566f, 0.566f, 0.566f, 1f) },
            { "Wood1Mat", new Color(0.467f, 0.296f, 0.168f, 1f) },
            { "Wood2Mat", new Color(0.545f, 0.361f, 0.208f, 1f) },
            { "CloudMat", new Color(1f, 1f, 1f, 1f) },
            { "DemoPlaneMat", new Color(0.424f, 0.711f, 0.267f, 1f) },
        };

        int count = 0;
        string folder = "Assets/SimpleLowPolyNature/Materials";
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { folder });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            if (colorMap.TryGetValue(mat.name, out Color color))
            {
                mat.SetColor("_BaseColor", color);
                mat.SetColor("_Color", color);
                mat.SetFloat("_Smoothness", 0f);
                EditorUtility.SetDirty(mat);
                count++;
                Debug.Log($"Restored color for {mat.name}: R={color.r:F2} G={color.g:F2} B={color.b:F2}");
            }
        }

        // Also fix emission materials
        string[] emissionNames = { "EmissionBlueMat", "EmissionYellowMat" };
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            if (mat.name == "EmissionBlueMat")
            {
                mat.SetColor("_BaseColor", new Color(0.2f, 0.4f, 0.9f, 1f));
                mat.SetColor("_Color", new Color(0.2f, 0.4f, 0.9f, 1f));
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(0.2f, 0.4f, 0.9f, 1f));
                mat.SetFloat("_Smoothness", 0f);
                EditorUtility.SetDirty(mat);
                count++;
            }
            else if (mat.name == "EmissionYellowMat")
            {
                mat.SetColor("_BaseColor", new Color(0.943f, 0.797f, 0.185f, 1f));
                mat.SetColor("_Color", new Color(0.943f, 0.797f, 0.185f, 1f));
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(0.943f, 0.797f, 0.185f, 1f));
                mat.SetFloat("_Smoothness", 0f);
                EditorUtility.SetDirty(mat);
                count++;
            }
        }

        // Also fix particle materials
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            if (mat.name == "FireParticleMat" || mat.name == "FireParticleAddMat")
            {
                mat.SetColor("_BaseColor", new Color(0.943f, 0.45f, 0.1f, 1f));
                mat.SetColor("_Color", new Color(0.943f, 0.45f, 0.1f, 1f));
                mat.SetFloat("_Smoothness", 0f);
                EditorUtility.SetDirty(mat);
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Restored colors for {count} materials.");
    }
}