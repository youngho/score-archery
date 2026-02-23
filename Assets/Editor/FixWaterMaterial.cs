using UnityEngine;
using UnityEditor;

public class FixWaterMaterial
{
    [MenuItem("Tools/Fix Water Material")]
    public static void FixWater()
    {
        string path = "Assets/SimpleLowPolyNature/Materials/WaterMat.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null)
        {
            Shader urpShader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (urpShader == null) urpShader = Shader.Find("Universal Render Pipeline/Lit");
            
            mat.shader = urpShader;
            // Set base color to the previous DeepColor (approximate)
            mat.SetColor("_BaseColor", new Color(0.25f, 0.72f, 0.93f, 0.8f));
            
            // Try to make it transparent if using Lit
            mat.SetFloat("_Surface", 1); // 1 = Transparent
            mat.SetFloat("_Blend", 0);   // 0 = Alpha, 1 = Premultiply
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            Debug.Log("Fixed WaterMat shader to URP.");
        }
        else
        {
            Debug.LogError("Could not find WaterMat");
        }
    }
}