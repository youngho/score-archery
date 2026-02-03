using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

/// <summary>
/// 스테이지 종료 시 API로 점수를 기록하는 서비스.
/// </summary>
public class StageScoreApiService : MonoBehaviour
{
    private const string ApiBaseUrl = "http://158.179.161.203:8080/api/stages";

    [Serializable]
    private class RecordRequest
    {
        public string publicId;
        public string nickname;
        public int worldNumber;
        public int stageNumber;
        public long score;
        public int starsEarned;
        public int playDurationSeconds;
        public string difficulty;
    }

    [Serializable]
    private class RecordResponse
    {
        public bool success;
        public long highScore;
        public bool isNewRecord;
        public int totalCompletions;
    }

    /// <summary>
    /// 현재 씬 이름에서 월드/스테이지 번호를 파싱 (예: 01HayBarn -> world 1, stage 1)
    /// </summary>
    public static bool TryParseStageFromScene(string sceneName, out int worldNumber, out int stageNumber)
    {
        worldNumber = 1;
        stageNumber = 1;

        if (string.IsNullOrEmpty(sceneName)) return false;

        // 01HayBarn, 02Field, 04Balloon 등 형태에서 앞 2자리 숫자 추출
        if (sceneName.Length >= 2 && char.IsDigit(sceneName[0]) && char.IsDigit(sceneName[1]))
        {
            if (int.TryParse(sceneName.Substring(0, 2), out int num))
            {
                stageNumber = num;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 스테이지 점수를 API에 기록 (코루틴)
    /// </summary>
    /// <param name="worldNumber">월드 번호 (1부터)</param>
    /// <param name="stageNumber">스테이지 번호 (1부터)</param>
    /// <param name="score">점수</param>
    /// <param name="starsEarned">별 개수 (0~3, 선택)</param>
    /// <param name="playDurationSeconds">플레이 시간(초), 선택</param>
    /// <param name="callback">완료 콜백 (성공여부, 응답메시지)</param>
    public IEnumerator RecordScoreAsync(
        int worldNumber,
        int stageNumber,
        long score,
        int starsEarned = 0,
        int playDurationSeconds = 0,
        Action<bool, string> callback = null)
    {
        string publicId = UserAccountManagerScript.Instance != null
            ? UserAccountManagerScript.Instance.GetPublicId()
            : PlayerPrefs.GetString("UserAccountPublicId", "");

        string nickname = UserAccountManagerScript.Instance != null
            ? UserAccountManagerScript.Instance.Nickname
            : PlayerPrefs.GetString("UserAccountNickname", "Player");

        if (string.IsNullOrEmpty(publicId))
        {
            Debug.LogWarning("[StageScoreApiService] No publicId - skipping API record");
            callback?.Invoke(false, "No public ID");
            yield break;
        }

        var requestBody = new RecordRequest
        {
            publicId = publicId,
            nickname = nickname,
            worldNumber = worldNumber,
            stageNumber = stageNumber,
            score = score,
            starsEarned = starsEarned,
            playDurationSeconds = playDurationSeconds,
            difficulty = "normal"
        };

        string json = JsonUtility.ToJson(requestBody);

        using (var request = new UnityWebRequest(ApiBaseUrl + "/record", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[StageScoreApiService] Record failed: {request.error}\n{request.downloadHandler?.text}");
                callback?.Invoke(false, request.error);
            }
            else
            {
                try
                {
                    RecordResponse response = JsonUtility.FromJson<RecordResponse>(request.downloadHandler.text);
                    Debug.Log($"[StageScoreApiService] Score recorded: success={response.success}, highScore={response.highScore}, isNewRecord={response.isNewRecord}");
                    callback?.Invoke(response.success, "OK");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[StageScoreApiService] Parse response failed: {ex.Message}");
                    callback?.Invoke(false, ex.Message);
                }
            }
        }
    }

    /// <summary>
    /// 현재 씬·ScoreManager 기준으로 점수를 기록한 뒤 다음 액션 실행
    /// </summary>
    public void RecordCurrentStageAndThen(Action onComplete)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (!TryParseStageFromScene(sceneName, out int worldNum, out int stageNum))
        {
            Debug.Log($"[StageScoreApiService] Could not parse stage from scene: {sceneName}");
            onComplete?.Invoke();
            return;
        }

        long score = ScoreManager.Instance != null ? GetCurrentScore() : 0;
        StartCoroutine(RecordAndContinue(worldNum, stageNum, score, onComplete));
    }

    private int GetCurrentScore()
    {
        return ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
    }

    private IEnumerator RecordAndContinue(int worldNum, int stageNum, long score, Action onComplete)
    {
        bool done = false;
        yield return RecordScoreAsync(worldNum, stageNum, score, 0, 0, (success, msg) => { done = true; });
        while (!done) yield return null;
        onComplete?.Invoke();
    }
}
