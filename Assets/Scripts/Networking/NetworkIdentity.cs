using UnityEngine;

public class NetworkIdentity : MonoBehaviour
{
    [SerializeField] private string id;

    public void SetId(string newId)
    {
        id = newId;
    }

    public string GetId()
    {
        return id;
    }
}
