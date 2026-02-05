using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;

[CreateAssetMenu(fileName = "UserAccountManagerScript", menuName = "Account/UserAccountManagerScript")]
public class UserAccountManagerScript : ScriptableObject
{
    private const string PublicIdKey = "UserAccountPublicId";
    private const string NicknameKey = "UserAccountNickname";
    private const string ManagerPath = "UserAccountManagerScript";
    private const string ApiBaseUrl = "http://158.179.161.203:8080/score/api/users"; // Adjust if necessary
    // private const string ApiBaseUrl = "http://localhost:8081/api/users"; // Adjust if necessary

    [SerializeField] private UserAccountDataScript accountData;

    [Serializable]
    public class RegisterRequest
    {
        public string nickname;
        public string publicId;
    }

    [Serializable]
    public class UserResponse
    {
        public string publicId;
        public string nickname;
        public string createdAt;
    }

    [Serializable]
    public class ChangeNicknameRequest
    {
        public string nickname;
    }

    [Serializable]
    public class ChangeNicknameResponse
    {
        public string publicId;
        public string nickname;
        // Add other fields if necessary based on API, but these are sufficient for updating local state
    }

    private class CoroutineRunner : MonoBehaviour { }

    public static UserAccountManagerScript Instance { get; private set; }
    private CoroutineRunner _coroutineRunner;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeOnLoad()
    {
        Debug.Log("[UserAccountManagerScript] Auto-initializing user account system...");
        
        UserAccountManagerScript manager = Resources.Load<UserAccountManagerScript>(ManagerPath);
        if (manager == null)
        {
            Debug.LogWarning("[UserAccountManagerScript] Manager asset not found in Resources.");
            return;
        }

        Instance = manager;
        manager.Initialize();
        
    }

    public string Nickname => accountData != null ? accountData.nickname : PlayerPrefs.GetString(NicknameKey);

    public void Initialize()
    {
        if (_coroutineRunner == null)
        {
            GameObject runnerGo = new GameObject("UserAccountCoroutineRunner");
            GameObject.DontDestroyOnLoad(runnerGo);
            _coroutineRunner = runnerGo.AddComponent<CoroutineRunner>();
        }

        // 1) PlayerPrefs에서 저장된 사용자 정보 로드
        LoadUserInfoFromPlayerPrefs();

        _coroutineRunner.StartCoroutine(AutoLoginOrRegister());
    }

    /// <summary>
    /// PlayerPrefs에 저장된 사용자 정보(publicId, nickname)를 불러와 메모리/accountData에 반영합니다.
    /// </summary>
    private void LoadUserInfoFromPlayerPrefs()
    {
        string publicId = PlayerPrefs.GetString(PublicIdKey, string.Empty);
        string nickname = PlayerPrefs.GetString(NicknameKey, string.Empty);

        if (!string.IsNullOrEmpty(publicId))
        {
            if (accountData != null)
                accountData.SetAccount(publicId, nickname);
            Debug.Log($"[UserAccountManagerScript] Loaded user from PlayerPrefs: {publicId} ({nickname})");
        }
    }

    /// <summary>
    /// 1) 저장된 사용자 정보로 로그인 시도 → 2) 실패 시 새로 등록 → 3) 성공 시 사용자 정보를 PlayerPrefs에 저장
    /// </summary>
    private IEnumerator AutoLoginOrRegister()
    {
        string existingId = PlayerPrefs.GetString(PublicIdKey, string.Empty);

        // 저장된 ID가 있으면 로그인 시도
        if (!string.IsNullOrEmpty(existingId))
        {
            Debug.Log($"[UserAccountManagerScript] Found existing Public ID: {existingId}. Attempting Login...");
            bool loginSuccess = false;
            yield return Login(existingId, (success, message) =>
            {
                loginSuccess = success;
                if (!success)
                    Debug.LogWarning($"[UserAccountManagerScript] Login failed: {message}. Proceeding to Register new account.");
            });

            if (loginSuccess)
            {
                // 로그인 성공 시 서버에서 받은 정보로 PlayerPrefs 갱신 (UpdateLocalAccount에서 이미 저장됨)
                yield break;
            }
        }

        // ID 없음 또는 로그인 실패 → 새 사용자 등록
        Debug.Log("[UserAccountManagerScript] No valid account (or login failed). Registering new user...");
        string defaultNickname = "Player_" + UnityEngine.Random.Range(1000, 9999);
        string publicId = ToBase62(Guid.NewGuid());

        yield return Register(defaultNickname, publicId, (success, msg) =>
        {
            if (success)
            {
                Debug.Log($"[UserAccountManagerScript] Auto-registration successful. Nickname: {defaultNickname}");
                UpdateLocalAccount(publicId, defaultNickname);
            }
            else
            {
                Debug.LogError($"[UserAccountManagerScript] Auto-registration failed: {msg}");
            }
        });

        // 등록 성공 시 Register() 내부에서 UpdateLocalAccount()로 이미 PlayerPrefs에 저장됨
    }

    public IEnumerator Login(string publicId, Action<bool, string> callback)
    {
        string url = $"{ApiBaseUrl}/{publicId}";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                 // Could be 404 if user deleted, or network error
                 callback?.Invoke(false, request.error);
            }
            else
            {
                try 
                {
                    UserResponse response = JsonUtility.FromJson<UserResponse>(request.downloadHandler.text);
                    Debug.Log($"[UserAccountManagerScript] Login successful for: {response.nickname} ({response.publicId})");
                    
                    // Update local state
                    UpdateLocalAccount(response.publicId, response.nickname);
                    
                    callback?.Invoke(true, "Success");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[UserAccountManagerScript] Failed to parse login response: {e.Message}");
                    callback?.Invoke(false, "Parse Error");
                }
            }
        }
    }

    /// <summary>
    /// 로그인/등록 성공 시 사용자 정보를 PlayerPrefs에 저장하고 accountData를 갱신합니다.
    /// </summary>
    private void UpdateLocalAccount(string publicId, string nickname)
    {
        PlayerPrefs.SetString(PublicIdKey, publicId);
        PlayerPrefs.SetString(NicknameKey, nickname);
        PlayerPrefs.Save();

        if (accountData != null)
            accountData.SetAccount(publicId, nickname);
    }

    public IEnumerator Register(string nickname, string publicId, Action<bool, string> callback)
    {
        RegisterRequest requestBody = new RegisterRequest
        {
            nickname = nickname,
            publicId = publicId
        };

        string json = JsonUtility.ToJson(requestBody);
        
        using (UnityWebRequest request = new UnityWebRequest(ApiBaseUrl + "/register", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[UserAccountManagerScript] Registration failed: {request.error}\n{request.downloadHandler.text}");
                callback?.Invoke(false, request.error);
            }
            else
            {
                UserResponse response = JsonUtility.FromJson<UserResponse>(request.downloadHandler.text);
                string publicIdStr = response.publicId.ToString();
                Debug.Log($"[UserAccountManagerScript] User registered successfully: {publicIdStr}");
                
                UpdateLocalAccount(publicIdStr, response.nickname);
                
                callback?.Invoke(true, "Success");
            }
        }
    }

    public IEnumerator ChangeNickname(string newNickname, Action<bool, string> callback)
    {
        string publicId = GetPublicId();
        if (string.IsNullOrEmpty(publicId))
        {
            callback?.Invoke(false, "No Public ID found");
            yield break;
        }

        string url = $"{ApiBaseUrl}/{publicId}/nickname";
        ChangeNicknameRequest requestBody = new ChangeNicknameRequest
        {
            nickname = newNickname
        };

        string json = JsonUtility.ToJson(requestBody);

        using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[UserAccountManagerScript] Change Nickname failed: {request.error}\n{request.downloadHandler.text}");
                callback?.Invoke(false, request.error);
            }
            else
            {
                ChangeNicknameResponse response = JsonUtility.FromJson<ChangeNicknameResponse>(request.downloadHandler.text);
                Debug.Log($"[UserAccountManagerScript] Nickname changed successfully: {response.nickname}");

                UpdateLocalAccount(publicId, response.nickname);

                callback?.Invoke(true, response.nickname);
            }
        }
    }

    public string GetPublicId()
    {
        return PlayerPrefs.GetString(PublicIdKey, string.Empty);
    }

    private static string ToBase62(Guid guid)
    {
        const string alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        byte[] bytes = guid.ToByteArray();

        // Append a zero byte to ensure the BigInteger interprets the value as positive
        byte[] posBytes = new byte[bytes.Length + 1];
        Array.Copy(bytes, posBytes, bytes.Length);
        posBytes[bytes.Length] = 0;

        System.Numerics.BigInteger dividend = new System.Numerics.BigInteger(posBytes);
        System.Text.StringBuilder builder = new System.Text.StringBuilder();

        if (dividend == 0)
        {
            return "0";
        }

        while (dividend != 0)
        {
            dividend = System.Numerics.BigInteger.DivRem(dividend, 62, out System.Numerics.BigInteger remainder);
            builder.Insert(0, alphabet[(int)remainder]);
        }

        return builder.ToString();
    }


}
