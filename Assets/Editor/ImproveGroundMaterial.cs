using UnityEngine;
using UnityEditor;

public class ImproveGroundMaterial : Editor
{
    [MenuItem("Tools/Enhance Ground Material Realism")]
    public static void ApplyRealisticSettings()
    {
        // 대상 재질 경로
        string materialPath = "Assets/YughuesFreeConcreteMaterials/Materials/M_YFCM_Honeycombing.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

        if (mat == null)
        {
            Debug.LogError($"재질을 찾을 수 없습니다: {materialPath}");
            return;
        }

        Undo.RecordObject(mat, "Enhance Ground Realism");

        // 1. 빛 반사 줄이기 (야외 콘크리트 느낌)
        mat.SetFloat("_Smoothness", 0.15f);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.15f);
        if (mat.HasProperty("_GlossMapScale")) mat.SetFloat("_GlossMapScale", 0.15f);

        // 2. 울퉁불퉁 입체감 살리기 (Normal 맵 강도 조절)
        mat.SetFloat("_BumpScale", 1.8f);

        // 3. 타일링 조절 (Ground 크기에 맞게 촘촘하게 무늬 반복)
        // 지형 스케일이 x10, z20 정도이므로, 무늬가 너무 크면 비현실적임.
        // BaseMap(Albedo)와 BumpMap(Normal) 모두 동일한 사이즈로 타일링 조절
        Vector2 newTiling = new Vector2(5f, 5f);
        if (mat.HasProperty("_BaseMap")) mat.SetTextureScale("_BaseMap", newTiling);
        if (mat.HasProperty("_MainTex")) mat.SetTextureScale("_MainTex", newTiling);
        if (mat.HasProperty("_BumpMap")) mat.SetTextureScale("_BumpMap", newTiling);

        // 변경사항 저장
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();

        Debug.Log("Ground 재질(M_YFCM_Honeycombing)의 '빛 반사', '질감 입체도', '타일링'을 현실적으로 강화했습니다!");
    }
}