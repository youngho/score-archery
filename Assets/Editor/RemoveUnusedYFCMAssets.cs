using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public class RemoveUnusedYFCMAssets : EditorWindow
{
    private string targetFolderPath = "Assets/YughuesFreeConcreteMaterials";
    private List<string> unusedAssets = new List<string>();
    private bool scanDone = false;

    [MenuItem("Tools/Remove Unused YFCM Assets")]
    public static void ShowWindow()
    {
        GetWindow<RemoveUnusedYFCMAssets>("Remove Unused YFCM Assets");
    }

    private void OnGUI()
    {
        GUILayout.Label("Unused Asset Remover for YFCM", EditorStyles.boldLabel);
        
        EditorGUILayout.HelpBox(
            "현재 열려있는 씬(Scene)에서 사용하지 않는 '" + targetFolderPath + "' 폴더 내의 재질(Material)과 텍스처(Texture)를 찾아 삭제합니다.", 
            MessageType.Info);

        if (GUILayout.Button("1. Scan Unused Assets"))
        {
            ScanUnusedAssets();
        }

        if (scanDone)
        {
            GUILayout.Space(10);
            GUILayout.Label($"발견된 미사용 에셋: {unusedAssets.Count}개", EditorStyles.boldLabel);
            
            if (unusedAssets.Count > 0)
            {
                if (GUILayout.Button("2. Delete All Unused Assets (Warning!)"))
                {
                    DeleteUnusedAssets();
                }

                // 스크롤 뷰로 목록 보여주기 (간단히 표시)
                GUILayout.Label("삭제 대기 목록 (상위 10개만 표시):");
                for (int i = 0; i < Mathf.Min(unusedAssets.Count, 10); i++)
                {
                    GUILayout.Label("- " + Path.GetFileName(unusedAssets[i]));
                }
                if (unusedAssets.Count > 10) GUILayout.Label("... 외 " + (unusedAssets.Count - 10) + "개 더 있음");
            }
            else
            {
                GUILayout.Label("삭제할 에셋이 없습니다. (모두 사용 중이거나 이미 지워짐)");
            }
        }
    }

    private void ScanUnusedAssets()
    {
        unusedAssets.Clear();

        // 1. 타겟 폴더 내의 모든 재질(Material)과 텍스처(Texture) 경로 수집
        string[] allAssetGuids = AssetDatabase.FindAssets("t:Material t:Texture", new[] { targetFolderPath });
        HashSet<string> allYFCMPaths = new HashSet<string>();
        foreach (string guid in allAssetGuids)
        {
            allYFCMPaths.Add(AssetDatabase.GUIDToAssetPath(guid));
        }

        // 2. 현재 씬에서 사용 중인 에셋(Dependencies) 수집
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        HashSet<string> usedPaths = new HashSet<string>();

        // 씬 내의 모든 게임오브젝트에서 컴포넌트 추출 (Renderer 등)
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>()
            .Where(go => go.scene.isLoaded).ToArray();

        // EditorUtility.CollectDependencies 를 사용하여 씬 객체들이 참조하는 모든 에셋 수집
        Object[] dependencies = EditorUtility.CollectDependencies(allObjects);

        foreach (Object obj in dependencies)
        {
            if (obj != null)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path))
                {
                    usedPaths.Add(path);
                }
            }
        }

        // 3. (전체 에셋) - (사용 중인 에셋) = (미사용 에셋) 추출
        foreach (string path in allYFCMPaths)
        {
            if (!usedPaths.Contains(path))
            {
                unusedAssets.Add(path);
            }
        }

        scanDone = true;
        Debug.Log($"스캔 완료: 총 {allYFCMPaths.Count}개의 YFCM 에셋 중 {unusedAssets.Count}개가 현재 씬에서 미사용 중입니다.");
    }

    private void DeleteUnusedAssets()
    {
        if (EditorUtility.DisplayDialog("Confirm Delete", 
            $"{unusedAssets.Count}개의 미사용 에셋을 휴지통으로 이동합니다.\n정말 삭제하시겠습니까?", "Yes, Delete", "Cancel"))
        {
            int deletedCount = 0;
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (string path in unusedAssets)
                {
                    if (AssetDatabase.MoveAssetToTrash(path))
                    {
                        deletedCount++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[Cleanup] {deletedCount}개의 미사용 에셋을 삭제했습니다.");
            scanDone = false;
            unusedAssets.Clear();
        }
    }
}