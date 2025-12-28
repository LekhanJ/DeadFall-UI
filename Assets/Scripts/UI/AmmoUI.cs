using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Display-only UI - shows server-authoritative weapon and ammo state
/// Now with real-time updates!
/// </summary>
public class AmmoUI : MonoBehaviour
{
    [Header("Current Weapon Display")]
    [SerializeField] private TextMeshProUGUI currentAmmoText;
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private Image weaponIcon;

    [Header("Reserve Ammo Display")]
    [SerializeField] private TextMeshProUGUI pistolAmmoText;
    [SerializeField] private TextMeshProUGUI rifleAmmoText;
    [SerializeField] private TextMeshProUGUI sniperAmmoText;
    [SerializeField] private TextMeshProUGUI shotgunAmmoText;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color lowAmmoColor = Color.yellow;
    [SerializeField] private Color noAmmoColor = Color.red;
    [SerializeField] private Color activeAmmoColor = new Color(0.3f, 1f, 0.3f);
    [SerializeField] private Color reloadingColor = Color.cyan;

    private WeaponController weaponController;
    private InventorySystem inventory;
    private AmmoManager ammoManager;

    // Cached ammo values for immediate display
    private int pistolAmmo = 0;
    private int rifleAmmo = 0;
    private int sniperAmmo = 0;
    private int shotgunAmmo = 0;

    void Start()
    {
        // Try to find local player immediately
        FindLocalPlayer();
    }

    void Update()
    {
        // Auto-find local player components if not found
        if (weaponController == null && NetworkClient.ClientID != null)
        {
            FindLocalPlayer();
        }

        if (weaponController != null && inventory != null)
        {
            UpdateDisplay();
        }
    }

    void FindLocalPlayer()
    {
        if (NetworkClient.ClientID == null) return;

        GameObject localPlayer = GameObject.Find("Player_" + NetworkClient.ClientID + "_LOCAL");
        if (localPlayer != null)
        {
            weaponController = localPlayer.GetComponent<WeaponController>();
            inventory = localPlayer.GetComponent<InventorySystem>();
            ammoManager = localPlayer.GetComponent<AmmoManager>();

            if (ammoManager != null)
            {
                // Initialize with current ammo values
                pistolAmmo = ammoManager.GetAmmo(AmmoType.PistolAmmo);
                rifleAmmo = ammoManager.GetAmmo(AmmoType.RifleAmmo);
                sniperAmmo = ammoManager.GetAmmo(AmmoType.SniperAmmo);
                shotgunAmmo = ammoManager.GetAmmo(AmmoType.ShotgunShells);
                
                Debug.Log($"AmmoUI found local player and initialized ammo display");
            }
        }
    }

    void UpdateDisplay()
    {
        UpdateCurrentWeaponDisplay();
        UpdateReserveAmmoDisplay();
    }

    void UpdateCurrentWeaponDisplay()
    {
        InventoryItem currentItem = inventory.GetCurrentItem();
        
        if (currentItem == null || currentItem.itemType != "Weapon")
        {
            // Hide weapon display
            if (currentAmmoText != null) currentAmmoText.text = "";
            if (weaponNameText != null) weaponNameText.text = "";
            if (weaponIcon != null) weaponIcon.enabled = false;
            return;
        }

        // Show weapon name
        if (weaponNameText != null)
        {
            weaponNameText.text = weaponController.GetCurrentWeaponName();
        }

        // Show weapon icon
        if (weaponIcon != null)
        {
            WeaponData weaponData = GetWeaponData(currentItem.weaponName);
            if (weaponData != null && weaponData.weaponIcon != null)
            {
                weaponIcon.sprite = weaponData.weaponIcon;
                weaponIcon.enabled = true;
            }
        }

        // Show current/magazine ammo
        if (currentAmmoText != null)
        {
            int current = weaponController.GetCurrentAmmo();
            int reserve = weaponController.GetReserveAmmo();
            int magazineCapacity = weaponController.GetMagazineCapacity();
            
            currentAmmoText.text = $"{current} / {reserve}";

            // Color based on ammo status
            if (weaponController.IsReloading())
            {
                currentAmmoText.text = "RELOADING...";
                currentAmmoText.color = reloadingColor;
            }
            else if (current == 0)
            {
                currentAmmoText.color = noAmmoColor;
            }
            else if (current <= magazineCapacity * 0.3f)
            {
                currentAmmoText.color = lowAmmoColor;
            }
            else
            {
                currentAmmoText.color = normalColor;
            }
        }
    }

    void UpdateReserveAmmoDisplay()
    {
        string currentAmmoType = GetCurrentWeaponAmmoType();

        // Update pistol ammo
        UpdateAmmoTypeText(
            pistolAmmoText,
            pistolAmmo,
            120,
            currentAmmoType == "pistol"
        );

        // Update rifle ammo
        UpdateAmmoTypeText(
            rifleAmmoText,
            rifleAmmo,
            90,
            currentAmmoType == "rifle"
        );

        // Update sniper ammo
        UpdateAmmoTypeText(
            sniperAmmoText,
            sniperAmmo,
            30,
            currentAmmoType == "sniper"
        );

        // Update shotgun ammo
        UpdateAmmoTypeText(
            shotgunAmmoText,
            shotgunAmmo,
            24,
            currentAmmoType == "shotgun"
        );
    }

    void UpdateAmmoTypeText(TextMeshProUGUI text, int amount, int max, bool isActive)
    {
        if (text == null) return;

        text.text = $"{amount}";

        // Highlight active ammo type
        if (isActive)
        {
            text.color = activeAmmoColor;
        }
        else
        {
            // Color based on amount
            if (amount == 0)
            {
                text.color = noAmmoColor;
            }
            else if (amount <= max * 0.3f)
            {
                text.color = lowAmmoColor;
            }
            else
            {
                text.color = normalColor;
            }
        }
    }

    string GetCurrentWeaponAmmoType()
    {
        if (inventory == null) return "";

        InventoryItem currentItem = inventory.GetCurrentItem();
        if (currentItem == null || currentItem.itemType != "Weapon") return "";

        WeaponData weaponData = GetWeaponData(currentItem.weaponName);
        if (weaponData == null) return "";

        // Map weapon to ammo type
        switch (weaponData.weaponName)
        {
            case "Pistol":
            case "SMG":
                return "pistol";
            case "Rifle":
                return "rifle";
            case "Sniper":
                return "sniper";
            case "Shotgun":
                return "shotgun";
            default:
                return "";
        }
    }

    WeaponData GetWeaponData(string weaponName)
    {
        if (string.IsNullOrEmpty(weaponName)) return null;
        return Resources.Load<WeaponData>($"Weapons/{weaponName}");
    }

    // Called by NetworkClient OR AmmoManager when ammo changes
    public void UpdateAmmoFromServer(int pistol, int rifle, int sniper, int shotgun)
    {
        // Store values immediately for display
        pistolAmmo = pistol;
        rifleAmmo = rifle;
        sniperAmmo = sniper;
        shotgunAmmo = shotgun;

        Debug.Log($"AmmoUI updated: Pistol={pistol}, Rifle={rifle}, Sniper={sniper}, Shotgun={shotgun}");

        // Force immediate UI refresh
        if (weaponController != null && inventory != null)
        {
            UpdateDisplay();
        }
    }
}