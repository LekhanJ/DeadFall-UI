using UnityEngine;

public class CollisionDestroy : MonoBehaviour
{
    [SerializeField] NetworkIdentity networkIdentity;
    [SerializeField] WhoActivatedMe whoActivatedMe;

    public void OnCollisionEnter2D(Collision2D collision) {
        NetworkIdentity ni = collision.gameObject.GetComponent<NetworkIdentity>();

        if (ni == null || ni.GetId() != whoActivatedMe.GetActivator()) {
            NetworkClient.Instance.SendCollisionDestroy(networkIdentity.GetId());
        }
    }
}
