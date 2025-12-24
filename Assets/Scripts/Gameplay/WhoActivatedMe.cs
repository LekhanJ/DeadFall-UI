using UnityEngine;

public class WhoActivatedMe : MonoBehaviour
{
    [SerializeField] private string whoActivatedMe;

    public void SetActivator(string ID) {
        whoActivatedMe = ID;
    }

    public string GetActivator() {
        return whoActivatedMe;
    }
}
