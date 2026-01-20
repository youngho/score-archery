using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;

[CreateAssetMenu(fileName = "UserAccountManagerScript", menuName = "Account/UserAccountManagerScript")]
public class UserAccountManagerScript : ScriptableObject
{
    private const string UserIdKey = "UserAccountId";
    private const string ManagerPath = "UserAccountManagerScript";
    private const string ApiBaseUrl = "http://localhost:8081/api/users"; // Adjust if necessary

    [SerializeField] private UserAccountDataScript accountData;

    [Serializable]
    public class RegisterRequest
    {
        public string username;
        public string email;
        public string password;
    }

    [Serializable]
    public class UserResponse
    {
        public long userId;
        public string username;
        public string email;
        public string createdAt;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeOnLoad()
    {
        Debug.Log("[UserAccountManagerScript] Auto-initializing user account system...");
        
        UserAccountManagerScript manager = Resources.Load<UserAccountManagerScript>(ManagerPath);
        if (manager == null)
        {
            Debug.LogWarning("[UserAccountManagerScript] Manager asset not found in Resources. Creating account via PlayerPrefs fallback.");
            CheckAndCreateAccount(null);
            return;
        }

        manager.Initialize();
    }

    public void Initialize()
    {
        CheckAndCreateAccount(accountData);
    }

    public static void CheckAndCreateAccount(UserAccountDataScript data)
    {
        string existingId = PlayerPrefs.GetString(UserIdKey, string.Empty);
        
        if (string.IsNullOrEmpty(existingId))
        {
            string newId = Guid.NewGuid().ToString();
            PlayerPrefs.SetString(UserIdKey, newId);
            PlayerPrefs.Save();
            
            if (data != null)
            {
                data.SetAccount(newId);
            }
            
            Debug.Log($"[UserAccountManagerScript] New local user account created: {newId}");
        }
        else
        {
            if (data != null)
            {
                data.SetAccount(existingId);
            }
            Debug.Log($"[UserAccountManagerScript] Existing user account found: {existingId}");
        }
    }

    public IEnumerator Register(string username, string email, string password, Action<bool, string> callback)
    {
        RegisterRequest requestBody = new RegisterRequest
        {
            username = username,
            email = email,
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
                string userIdStr = response.userId.ToString();
                Debug.Log($"[UserAccountManagerScript] User registered successfully: {userIdStr}");
                
                // Update local storage and data asset
                PlayerPrefs.SetString(UserIdKey, userIdStr);
                PlayerPrefs.Save();
                
                if (accountData != null)
                {
                    accountData.SetAccount(userIdStr, response.username, response.email);
                }
                
                callback?.Invoke(true, "Success");
            }
        }
    }

    public string GetUserId()
    {
        return PlayerPrefs.GetString(UserIdKey, string.Empty);
    }
}
