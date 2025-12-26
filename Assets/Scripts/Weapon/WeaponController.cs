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
    private Transform currentFirePoint;

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

        if (currentItem.itemType == "Hand")
        {
            HandleMeleeAttack();
        }
        else if (currentItem.itemType == "Weapon")
        {
            WeaponData weaponData = GetWeaponData(currentItem.weaponName);
            if (weaponData != null)
            {
                HandleGunShooting(weaponData);

                // Reload key
                if (Input.GetKeyDown(KeyCode.R))
                {
                    StartReload(weaponData);
                }
            }
        }
        // Note: Consumables (Health, Shield) are handled in InventorySystem with E key
        // Grenades are handled in InventorySystem with G key
    }

    void HandleMeleeAttack()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + 0.5f; // Melee cooldown

            PerformMeleePunch();
        }
    }

    void PerformMeleePunch()
    {
        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 punchDirection = (mouseWorld - transform.position).normalized;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, punchDirection, 1.5f);

        if (hit.collider != null && hit.collider.CompareTag("Player"))
        {
            Debug.Log("Punched: " + hit.collider.name);
            
            NetworkClient.Instance.SendMeleeAttack(hit.collider.name, 15f);
        }

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

        Vector3 firePointWorldPos = currentFirePoint.position;
        Vector2 direction = currentFirePoint.up;

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
        if (currentItem != null && currentItem.itemType == "Weapon")
        {
            WeaponData weaponData = GetWeaponData(currentItem.weaponName);
            if (weaponData != null)
            {
                currentAmmo = weaponData.magazineCapacity;
                isReloading = false;
                Debug.Log("Reload complete!");
            }
        }
    }

    WeaponData GetWeaponData(string weaponName)
    {
        if (string.IsNullOrEmpty(weaponName)) return null;
        return Resources.Load<WeaponData>($"Weapons/{weaponName}");
    }

    // Public getters for UI
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
        
        // Reset ammo when switching weapons
        if (firePoint != null)
        {
            InventoryItem currentItem = inventory.GetCurrentItem();
            if (currentItem != null && currentItem.itemType == "Weapon")
            {
                WeaponData weaponData = GetWeaponData(currentItem.weaponName);
                if (weaponData != null)
                {
                    currentAmmo = weaponData.magazineCapacity;
                }
            }
        }
    }
}