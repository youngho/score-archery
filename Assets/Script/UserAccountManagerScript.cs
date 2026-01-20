using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;

[CreateAssetMenu(fileName = "UserAccountManagerScript", menuName = "Account/UserAccountManagerScript")]
public class UserAccountManagerScript : ScriptableObject
{
    private const string PublicIdKey = "UserAccountPublicId";
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
        string existingId = PlayerPrefs.GetString(PublicIdKey, string.Empty);
        
        if (string.IsNullOrEmpty(existingId))
        {
            string newId = ToBase62(Guid.NewGuid());
            PlayerPrefs.SetString(PublicIdKey, newId);
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
                PlayerPrefs.Save();
                
                if (accountData != null)
                {
                    accountData.SetAccount(publicIdStr, response.nickname);
                }
                
                callback?.Invoke(true, "Success");
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
