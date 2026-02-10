using UnityEngine;
using TMPro;

/// <summary>
/// 99StageResult 씬 전용. StageResultData.LastScore를 scoreText에 표시.
/// </summary>
public class StageResultScoreDisplay : MonoBehaviour
{
    [Tooltip("표시할 TextMeshProUGUI (비어 있으면 'ScoreText' 이름으로 검색)")]
    public TextMeshProUGUI scoreText;

    private void Start()
    {
        if (scoreText == null)
        {
            var go = GameObject.Find("ScoreText");
            if (go != null)
                scoreText = go.GetComponent<TextMeshProUGUI>();
        }
        if (scoreText != null)
            scoreText.text = $"{StageResultData.LastScore}";
    }
}
