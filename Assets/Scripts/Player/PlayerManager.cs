using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerManager : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Aiming")]
    public Transform aimPivot;   

    Rigidbody2D rb;
    Camera cam;
    bool isLocal;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        cam = Camera.main;

        isLocal = gameObject.name.Contains("_LOCAL");

        if (!isLocal)
        {
            rb.simulated = false;
            enabled = false;
        }
    }

    void Update()
    {
        HandleAim();
    }

    void FixedUpdate()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 move = new Vector2(h, v).normalized;

        rb.linearVelocity = move * moveSpeed;
    }

    void HandleAim()
    {
        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = mouseWorld - transform.position;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

        aimPivot.rotation = Quaternion.Euler(0, 0, angle);

        NetworkClient.Instance.SendAim(dir.normalized);
    }
}