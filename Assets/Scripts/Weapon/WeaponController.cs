using UnityEngine;

/// <summary>
/// Client-side weapon controller - only sends requests to server
/// All logic is server-authoritative
/// NOW WITH REAL-TIME UI UPDATES!
/// </summary>
public class WeaponController : MonoBehaviour
{
    private InventorySystem inventory;
    private Camera cam;
    private bool isLocal;

    [Header("Visual Effects")]
    private Transform currentFirePoint;

    [Header("Weapon State (Server Authoritative)")]
    public int currentAmmo = 0;
    public int magazineCapacity = 0;
    public int reserveAmmo = 0;
    public bool isReloading = false;
    public string currentWeaponName = "";

    // Track last values to detect changes
    private int lastCurrentAmmo = -1;
    private int lastReserveAmmo = -1;
    private bool lastReloadingState = false;

    void Start()
    {
        inventory = GetComponent<InventorySystem>();
        cam = Camera.main;
        isLocal = gameObject.name.Contains("_LOCAL");
    }

    void Update()
    {
        if (!isLocal) return;

        HandleWeaponInput();
        CheckForUIUpdates();
    }

    void CheckForUIUpdates()
    {
        // Check if weapon state changed and trigger UI update
        if (currentAmmo != lastCurrentAmmo || 
            reserveAmmo != lastReserveAmmo || 
            isReloading != lastReloadingState)
        {
            lastCurrentAmmo = currentAmmo;
            lastReserveAmmo = reserveAmmo;
            lastReloadingState = isReloading;

            // Force UI update
            TriggerAmmoUIUpdate();
        }
    }

    void TriggerAmmoUIUpdate()
    {
        AmmoUI ammoUI = FindFirstObjectByType<AmmoUI>();
        if (ammoUI != null)
        {
            // This will trigger the UI to refresh on next Update()
            // The UI will read current weapon controller values
        }
    }

    void HandleWeaponInput()
    {
        InventoryItem currentItem = inventory.GetCurrentItem();
        
        if (currentItem == null) return;

        if (currentItem.itemType == "Hand")
        {
            HandleMeleeInput();
        }
        else if (currentItem.itemType == "Weapon")
        {
            HandleGunInput();
        }
    }

    void HandleMeleeInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
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

    void HandleGunInput()
    {
        // Shooting input
        bool shootPressed = Input.GetMouseButtonDown(0); // Semi-auto
        bool shootHeld = Input.GetMouseButton(0); // Full-auto

        if (shootPressed || shootHeld)
        {
            RequestShoot();
        }

        // Reload input
        if (Input.GetKeyDown(KeyCode.R))
        {
            RequestReload();
        }
    }

    void RequestShoot()
    {
        if (currentFirePoint == null)
        {
            Debug.LogWarning("No fire point available");
            return;
        }

        // Optimistic client prediction - assume we'll shoot successfully
        if (currentAmmo > 0 && !isReloading)
        {
            // Predict ammo consumption for instant feedback
            currentAmmo--;
            TriggerAmmoUIUpdate();
        }

        Vector3 firePointWorldPos = currentFirePoint.position;
        Vector2 direction = currentFirePoint.up;

        // Send shoot request to server
        NetworkClient.Instance.SendShootRequest(firePointWorldPos, direction);
    }

    void RequestReload()
    {
        if (isReloading)
        {
            Debug.Log("Already reloading");
            return;
        }

        // Send reload request to server
        NetworkClient.Instance.SendReloadRequest();
    }

    // Called by NetworkClient when server sends weapon state
    public void UpdateWeaponState(WeaponState state)
    {
        if (state == null) return;

        bool stateChanged = 
            currentWeaponName != state.weaponName ||
            currentAmmo != state.currentAmmo ||
            magazineCapacity != state.magazineCapacity ||
            reserveAmmo != state.reserveAmmo ||
            isReloading != state.isReloading;

        currentWeaponName = state.weaponName;
        currentAmmo = state.currentAmmo;
        magazineCapacity = state.magazineCapacity;
        reserveAmmo = state.reserveAmmo;
        isReloading = state.isReloading;

        if (stateChanged)
        {
            Debug.Log($"Weapon State Updated: {currentWeaponName} | Ammo: {currentAmmo}/{magazineCapacity} | Reserve: {reserveAmmo} | Reloading: {isReloading}");
            TriggerAmmoUIUpdate();
        }
    }

    public void OnReloadStarted(string weaponName, float reloadTime)
    {
        isReloading = true;
        Debug.Log($"Reloading {weaponName}... ({reloadTime}s)");
        TriggerAmmoUIUpdate();
    }

    public void OnReloadCompleted()
    {
        isReloading = false;
        Debug.Log("Reload complete!");
        TriggerAmmoUIUpdate();
    }

    public void SetFirePoint(Transform firePoint)
    {
        currentFirePoint = firePoint;
    }

    // Public getters for UI
    public int GetCurrentAmmo() => currentAmmo;
    public int GetMagazineCapacity() => magazineCapacity;
    public int GetReserveAmmo() => reserveAmmo;
    public bool IsReloading() => isReloading;
    public string GetCurrentWeaponName() => currentWeaponName;
}

[System.Serializable]
public class WeaponState
{
    public string weaponName;
    public string weaponType;
    public string ammoType;
    public int currentAmmo;
    public int magazineCapacity;
    public int reserveAmmo;
    public bool isReloading;
    public float reloadTimeRemaining;
    public float damage;
    public float fireRate;
}