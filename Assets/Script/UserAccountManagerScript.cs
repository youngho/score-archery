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

    public static UserAccountManagerScript Instance { get; private set; }

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

        Instance = manager;
        manager.Initialize();
    }

    public string Nickname => accountData != null ? accountData.nickname : PlayerPrefs.GetString(NicknameKey, "Player");

    public void Initialize()
    {
        CheckAndCreateAccount(accountData);
    }

    public static void CheckAndCreateAccount(UserAccountDataScript data)
    {
        string existingId = PlayerPrefs.GetString(PublicIdKey, string.Empty);
        string existingNickname = PlayerPrefs.GetString(NicknameKey, "Player");
        
        if (string.IsNullOrEmpty(existingId))
        {
            string newId = ToBase62(Guid.NewGuid());
            string defaultNickname = "Player";
            
            PlayerPrefs.SetString(PublicIdKey, newId);
            PlayerPrefs.SetString(NicknameKey, defaultNickname);
            PlayerPrefs.Save();
            
            if (data != null)
            {
                data.SetAccount(newId, defaultNickname);
            }
            
            Debug.Log($"[UserAccountManagerScript] New local user account created: {newId}, Nickname: {defaultNickname}");
        }
        else
        {
            if (data != null)
            {
                data.SetAccount(existingId, existingNickname);
            }
            Debug.Log($"[UserAccountManagerScript] Existing user account found: {existingId}, Nickname: {existingNickname}");
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
                
                // Update local storage and data asset
                PlayerPrefs.SetString(PublicIdKey, publicIdStr);
                PlayerPrefs.SetString(NicknameKey, response.nickname);
                PlayerPrefs.Save();
                
                if (accountData != null)
                {
                    accountData.SetAccount(publicIdStr, response.nickname);
                }
                
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

                // Update local storage and data asset
                PlayerPrefs.SetString(NicknameKey, response.nickname);
                PlayerPrefs.Save();

                if (accountData != null)
                {
                    accountData.SetAccount(publicId, response.nickname);
                }

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
