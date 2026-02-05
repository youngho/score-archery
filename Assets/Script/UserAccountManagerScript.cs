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
    // private const string ApiBaseUrl = "http://158.179.161.203:8080/score/api/users"; // Adjust if necessary
    private const string ApiBaseUrl = "http://localhost:8081/api/users"; // Adjust if necessary

    [SerializeField] private UserAccountDataScript accountData;

    [Serializable]
    public class RegisterRequest
    {
        public string nickname;
        public string password;
    }

    [Serializable]
    public class UserResponse
    {
        public long publicId;
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

    public string Nickname => accountData != null ? accountData.nickname : PlayerPrefs.GetString(NicknameKey, "Player");

    public void Initialize()
    {
        if (_coroutineRunner == null)
        {
            GameObject runnerGo = new GameObject("UserAccountCoroutineRunner");
            GameObject.DontDestroyOnLoad(runnerGo);
            _coroutineRunner = runnerGo.AddComponent<CoroutineRunner>();
        }

        _coroutineRunner.StartCoroutine(AutoLoginOrRegister());
    }

    private IEnumerator AutoLoginOrRegister()
    {
        string existingId = PlayerPrefs.GetString(PublicIdKey, string.Empty);
        
        if (!string.IsNullOrEmpty(existingId))
        {
            Debug.Log($"[UserAccountManagerScript] Found existing Public ID: {existingId}. Attempting Login...");
            bool loginSuccess = false;
            yield return Login(existingId, (success, message) => 
            {
                loginSuccess = success;
                if (!success)
                {
                    Debug.LogWarning($"[UserAccountManagerScript] Login failed: {message}. Proceeding to Register new account.");
                }
            });

            if (loginSuccess)
            {
                yield break; // Login successful, stop here
            }
        }

        // If no ID or Login failed, Register
        Debug.Log("[UserAccountManagerScript] No valid account (or login failed). Registering new user...");
        
        // Generate random nickname for initial registration
        string defaultNickname = "Player_" + UnityEngine.Random.Range(1000, 9999);
        string defaultPassword = Guid.NewGuid().ToString(); // Simple password generation

        yield return Register(defaultNickname, defaultPassword, (success, msg) => 
        {
             if (success)
             {
                 Debug.Log($"[UserAccountManagerScript] Auto-registration successful. Nickname: {defaultNickname}");
             }
             else
             {
                 Debug.LogError($"[UserAccountManagerScript] Auto-registration failed: {msg}");
             }
        });
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
                    UpdateLocalAccount(response.publicId.ToString(), response.nickname);
                    
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

    private void UpdateLocalAccount(string publicId, string nickname)
    {
        PlayerPrefs.SetString(PublicIdKey, publicId);
        PlayerPrefs.SetString(NicknameKey, nickname);
        PlayerPrefs.Save();
        
        if (accountData != null)
        {
            accountData.SetAccount(publicId, nickname);
        }
    }

    public IEnumerator Register(string nickname, string password, Action<bool, string> callback)
    {
        RegisterRequest requestBody = new RegisterRequest
        {
            nickname = nickname,
            password = password
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


}
