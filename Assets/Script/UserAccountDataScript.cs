using UnityEngine;

[CreateAssetMenu(fileName = "UserAccountDataScript", menuName = "Account/UserAccountDataScript")]
public class UserAccountDataScript : ScriptableObject
{
    public string publicId;
    public string username;
    public string email;
    public string createdAt;
    
    public bool HasAccount => !string.IsNullOrEmpty(publicId);

    public void SetAccount(string id, string name = "", string mail = "")
    {
        publicId = id;
        username = name;
        email = mail;
        createdAt = System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public void Clear()
    {
        publicId = string.Empty;
        username = string.Empty;
        email = string.Empty;
        createdAt = string.Empty;
    }
}
