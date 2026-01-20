using UnityEngine;

[CreateAssetMenu(fileName = "UserAccountDataScript", menuName = "Account/UserAccountDataScript")]
public class UserAccountDataScript : ScriptableObject
{
    public string userId;
    public string username;
    public string email;
    public string createdAt;
    
    public bool HasAccount => !string.IsNullOrEmpty(userId);

    public void SetAccount(string id, string name = "", string mail = "")
    {
        userId = id;
        username = name;
        email = mail;
        createdAt = System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public void Clear()
    {
        userId = string.Empty;
        username = string.Empty;
        email = string.Empty;
        createdAt = string.Empty;
    }
}
