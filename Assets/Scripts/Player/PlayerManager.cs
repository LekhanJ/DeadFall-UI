using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerManager : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Aiming")]
    public Transform aimPivot;   

    [Header("Shooting")]
    public Transform firePoint;

    Rigidbody2D rb;
    Camera cam;
    bool isLocal;

    // Shooting
    private Cooldown shootingCooldown;
    private BulletData bulletData;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        cam = Camera.main;

        shootingCooldown = new Cooldown();
        bulletData = new BulletData();
        bulletData.position = new Position();
        bulletData.direction = new Position();

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
        HandleShoot();
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
        Vector3 mouseWorld =
            cam.ScreenToWorldPoint(Input.mousePosition);

        Vector2 dir =
            mouseWorld - transform.position;

        float angle =
            Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

        aimPivot.rotation = Quaternion.Euler(0, 0, angle);

        NetworkClient.Instance.SendAim(dir.normalized);
    }

    void HandleShoot()
    {   
        shootingCooldown.CooldownUpdate();

        if (Input.GetMouseButtonDown(0) && !shootingCooldown.IsOnCooldown())
        {   
            shootingCooldown.StartCooldown();

            bulletData.position.x = firePoint.position.x.TwoDecimals();
            bulletData.position.y = firePoint.position.y.TwoDecimals();

            bulletData.direction.x = firePoint.up.x;
            bulletData.direction.y = firePoint.up.y;

            NetworkClient.Instance.SendShoot(bulletData);
        }
    }
}
