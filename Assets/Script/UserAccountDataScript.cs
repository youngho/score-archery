using UnityEngine;

[CreateAssetMenu(fileName = "UserAccountDataScript", menuName = "Account/UserAccountDataScript")]
public class UserAccountDataScript : ScriptableObject
{
    public string publicId;
    public string nickname;
    public string createdAt;
    
    public bool HasAccount => !string.IsNullOrEmpty(publicId);

    public void SetAccount(string id, string name = "")
    {
        publicId = id;
        nickname = name;
        createdAt = System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public void Clear()
    {
        publicId = string.Empty;
        nickname = string.Empty;
        createdAt = string.Empty;
    }
}
