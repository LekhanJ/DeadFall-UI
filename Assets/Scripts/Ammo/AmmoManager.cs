using UnityEngine;
using System.Collections.Generic;

public class AmmoManager : MonoBehaviour
{
    private Dictionary<AmmoType, int> ammoInventory = new Dictionary<AmmoType, int>();
    private bool isLocal;
    private AmmoUI cachedAmmoUI; // Cache the UI reference

    void Awake()
    {
        // Initialize all ammo types to 0
        ammoInventory[AmmoType.PistolAmmo] = 0;
        ammoInventory[AmmoType.RifleAmmo] = 0;
        ammoInventory[AmmoType.SniperAmmo] = 0;
        ammoInventory[AmmoType.ShotgunShells] = 0;
    }

    void Start()
    {
        isLocal = gameObject.name.Contains("_LOCAL");
        
        if (isLocal)
        {
            // Find and cache the AmmoUI
            cachedAmmoUI = FindFirstObjectByType<AmmoUI>();
            if (cachedAmmoUI == null)
            {
                Debug.LogError("AmmoUI not found! Make sure it exists in the scene.");
            }
            else
            {
                // Initialize UI with current values
                NotifyAmmoChanged();
            }
        }
    }

    public int GetAmmo(AmmoType type)
    {
        if (type == AmmoType.None) return -1;
        if (ammoInventory.ContainsKey(type))
        {
            return ammoInventory[type];
        }
        return 0;
    }

    public bool HasAmmo(AmmoType type, int amount = 1)
    {
        if (type == AmmoType.None) return true;
        return GetAmmo(type) >= amount;
    }

    public bool UseAmmo(AmmoType type, int amount = 1)
    {
        if (type == AmmoType.None) return true;
        if (!HasAmmo(type, amount)) return false;

        ammoInventory[type] -= amount;

        if (isLocal)
        {
            NotifyAmmoChanged();
        }

        return true;
    }

    public bool AddAmmo(AmmoType type, int amount)
    {
        if (type == AmmoType.None) return false;

        int current = GetAmmo(type);
        int max = AmmoConfig.GetMaxAmmo(type);

        if (current >= max)
        {
            Debug.Log($"Cannot add {type} ammo - already at max ({max})");
            return false;
        }

        int newAmount = Mathf.Min(current + amount, max);
        int actualAdded = newAmount - current;
        ammoInventory[type] = newAmount;

        Debug.Log($"✅ Added {actualAdded} {type} ammo. Total: {newAmount}/{max}");

        if (isLocal)
        {
            NotifyAmmoChanged();
        }

        return true;
    }

    public void SetAmmo(AmmoType type, int amount)
    {
        if (type == AmmoType.None) return;

        int max = AmmoConfig.GetMaxAmmo(type);
        int oldAmount = ammoInventory[type];
        ammoInventory[type] = Mathf.Clamp(amount, 0, max);

        Debug.Log($"SetAmmo: {type} changed from {oldAmount} to {ammoInventory[type]}");

        if (isLocal)
        {
            NotifyAmmoChanged();
        }
    }

    void NotifyAmmoChanged()
    {
        // Try to use cached reference first
        if (cachedAmmoUI == null)
        {
            cachedAmmoUI = FindFirstObjectByType<AmmoUI>();
        }

        if (cachedAmmoUI != null)
        {
            cachedAmmoUI.UpdateAmmoFromServer(
                GetAmmo(AmmoType.PistolAmmo),
                GetAmmo(AmmoType.RifleAmmo),
                GetAmmo(AmmoType.SniperAmmo),
                GetAmmo(AmmoType.ShotgunShells)
            );
        }
        else
        {
            Debug.LogWarning("⚠️ AmmoUI not found when trying to update! Make sure AmmoUI exists in scene.");
        }
    }

    public Dictionary<AmmoType, int> GetAllAmmo()
    {
        return new Dictionary<AmmoType, int>(ammoInventory);
    }

    // Server sync methods
    public void SyncAmmoFromServer(Dictionary<string, int> serverAmmo)
    {
        bool changed = false;

        if (serverAmmo.ContainsKey("pistol"))
        {
            ammoInventory[AmmoType.PistolAmmo] = serverAmmo["pistol"];
            changed = true;
        }
        if (serverAmmo.ContainsKey("rifle"))
        {
            ammoInventory[AmmoType.RifleAmmo] = serverAmmo["rifle"];
            changed = true;
        }
        if (serverAmmo.ContainsKey("sniper"))
        {
            ammoInventory[AmmoType.SniperAmmo] = serverAmmo["sniper"];
            changed = true;
        }
        if (serverAmmo.ContainsKey("shotgun"))
        {
            ammoInventory[AmmoType.ShotgunShells] = serverAmmo["shotgun"];
            changed = true;
        }

        if (changed && isLocal)
        {
            NotifyAmmoChanged();
        }
    }
}