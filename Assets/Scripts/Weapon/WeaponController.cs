using UnityEngine;

public class WeaponController : MonoBehaviour
{
    private InventorySystem inventory;
    private Camera cam;
    private bool isLocal;

    [Header("Weapon State")]
    private int currentAmmo;
    private bool isReloading = false;
    private float nextFireTime = 0f;
    private Transform currentFirePoint; // Current weapon's fire point

    void Start()
    {
        inventory = GetComponent<InventorySystem>();
        cam = Camera.main;
        isLocal = gameObject.name.Contains("_LOCAL");
    }

    void Update()
    {
        if (!isLocal) return;

        HandleWeaponActions();
    }

    void HandleWeaponActions()
    {
        InventoryItem currentItem = inventory.GetCurrentItem();
        
        if (currentItem == null) return;

        if (currentItem.itemType == ItemType.Melee)
        {
            HandleMeleeAttack();
        }
        else if (currentItem.itemType == ItemType.Gun)
        {
            HandleGunShooting(currentItem.weaponData);
        }

        // Reload key
        if (Input.GetKeyDown(KeyCode.R) && currentItem.itemType == ItemType.Gun)
        {
            StartReload(currentItem.weaponData);
        }
    }

    void HandleMeleeAttack()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + 0.5f; // Melee cooldown

            // Perform melee attack
            PerformMeleePunch();
        }
    }

    void PerformMeleePunch()
    {
        // Raycast or overlap circle to detect enemies in front
        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 punchDirection = (mouseWorld - transform.position).normalized;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, punchDirection, 1.5f);

        if (hit.collider != null && hit.collider.CompareTag("Player"))
        {
            Debug.Log("Punched: " + hit.collider.name);
            
            // Send melee attack to server
            NetworkClient.Instance.SendMeleeAttack(hit.collider.name, 15f); // 15 damage
        }

        // Visual feedback
        Debug.DrawRay(transform.position, punchDirection * 1.5f, Color.red, 0.5f);
    }

    void HandleGunShooting(WeaponData weapon)
    {
        if (weapon == null) return;

        if (isReloading) return;

        // Initialize ammo if needed
        if (currentAmmo == 0 && nextFireTime == 0f)
        {
            currentAmmo = weapon.magazineCapacity;
        }

        // Auto reload when empty
        if (currentAmmo <= 0)
        {
            StartReload(weapon);
            return;
        }

        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + weapon.fireRate;
            currentAmmo--;

            FireBullet(weapon);

            // UI update (if you have ammo counter)
            Debug.Log($"Ammo: {currentAmmo}/{weapon.magazineCapacity}");
        }
    }

    void FireBullet(WeaponData weapon)
    {
        if (currentFirePoint == null)
        {
            Debug.LogError("No fire point found on weapon!");
            return;
        }

        // Use the weapon's fire point position and direction
        Vector3 firePointWorldPos = currentFirePoint.position;
        Vector2 direction = currentFirePoint.up;

        // Send to server
        BulletData bulletData = new BulletData
        {
            id = System.Guid.NewGuid().ToString(),
            position = new Position 
            { 
                x = firePointWorldPos.x.TwoDecimals(), 
                y = firePointWorldPos.y.TwoDecimals() 
            },
            direction = new Position 
            { 
                x = direction.x, 
                y = direction.y 
            }
        };

        NetworkClient.Instance.SendShoot(bulletData, weapon.weaponName);
    }

    void StartReload(WeaponData weapon)
    {
        if (isReloading) return;
        if (currentAmmo == weapon.magazineCapacity) return;

        isReloading = true;
        Debug.Log("Reloading...");

        Invoke(nameof(FinishReload), weapon.reloadTime);
    }

    void FinishReload()
    {
        InventoryItem currentItem = inventory.GetCurrentItem();
        if (currentItem != null && currentItem.weaponData != null)
        {
            currentAmmo = currentItem.weaponData.magazineCapacity;
            isReloading = false;
            Debug.Log("Reload complete!");
        }
    }

    // Public getter for UI
    public int GetCurrentAmmo()
    {
        return currentAmmo;
    }

    public bool IsReloading()
    {
        return isReloading;
    }

    // Called by InventorySystem when weapon is equipped
    public void SetFirePoint(Transform firePoint)
    {
        currentFirePoint = firePoint;
    }
}