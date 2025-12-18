using UnityEngine;
using System.Collections.Generic;

public class InventorySystem : MonoBehaviour
{
    [Header("Inventory Settings")]
    public int maxSlots = 6;
    private List<InventoryItem> inventory = new List<InventoryItem>();
    private int currentSlotIndex = 0;

    [Header("Weapon Holder")]
    public Transform weaponHolder; // Child transform where weapons are visually held

    private GameObject currentWeaponObject;
    private bool isLocal;

    [SerializeField] private GameObject handPrefab;


    void Awake()
    {
        // Initialize inventory with empty slots
        for (int i = 0; i < maxSlots; i++)
        {
            inventory.Add(null);
        }

        // Slot 0 is always the hand (melee/punch)
        inventory[0] = new InventoryItem
        {
            itemType = ItemType.Melee,
            itemName = "Hand",
            weaponData = null
        };
    }

    void Start()
    {
        isLocal = gameObject.name.Contains("_LOCAL");

        if (isLocal)
        {
            // Example: Add starting weapons
            AddItemToSlot(1, CreatePistolItem());
            Debug.Log("Pistol added to slot 1");
            AddItemToSlot(2, CreateRifleItem());
        }

        EquipSlot(currentSlotIndex);
    }

    void Update()
    {
        if (!isLocal) return;

        HandleInventoryInput();
    }

    void HandleInventoryInput()
    {
        // Number keys 1-6 for inventory slots
        for (int i = 0; i < maxSlots; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SwitchToSlot(i);
            }
        }

        // Mouse scroll wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            SwitchToSlot((currentSlotIndex - 1 + maxSlots) % maxSlots);
        }
        else if (scroll < 0f)
        {
            SwitchToSlot((currentSlotIndex + 1) % maxSlots);
        }
    }

    public void SwitchToSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxSlots) return;
        if (slotIndex == currentSlotIndex) return;
        if (inventory[slotIndex] == null) return;

        currentSlotIndex = slotIndex;
        EquipSlot(currentSlotIndex);

        // Send to server
        NetworkClient.Instance.SendInventorySwitch(currentSlotIndex);
    }

    public void EquipSlot(int slotIndex)
    {
        // Destroy current weapon visual
        if (currentWeaponObject != null)
        {
            Destroy(currentWeaponObject);
        }

        InventoryItem item = inventory[slotIndex];
        if (item == null) return;

        // Spawn weapon visual
        if (item.itemType == ItemType.Gun && item.weaponData != null)
        {
            currentWeaponObject = Instantiate(item.weaponData.weaponPrefab, weaponHolder);
            currentWeaponObject.transform.localPosition = Vector3.zero;
            currentWeaponObject.transform.localRotation = Quaternion.identity;

            // Update WeaponController's fire point reference
            UpdateFirePointReference();
        }
        else if (item.itemType == ItemType.Melee)
        {
            // Hand has no visual, or you can add a fist sprite
            currentWeaponObject = Instantiate(handPrefab, weaponHolder);
            currentWeaponObject.transform.localPosition = Vector3.zero;
            currentWeaponObject.transform.localRotation = Quaternion.identity;
            
            // Clear fire point reference for melee
            WeaponController weaponController = GetComponent<WeaponController>();
            if (weaponController != null)
            {
                weaponController.SetFirePoint(null);
            }
        }
    }

    void UpdateFirePointReference()
    {
        if (currentWeaponObject == null) return;

        // Find the "FirePoint" child transform in the weapon
        Transform firePoint = currentWeaponObject.transform.Find("FirePoint");
        
        if (firePoint == null)
        {
            Debug.LogWarning($"Weapon {currentWeaponObject.name} is missing a 'FirePoint' child transform!");
        }

        // Update WeaponController's reference
        WeaponController weaponController = GetComponent<WeaponController>();
        if (weaponController != null)
        {
            weaponController.SetFirePoint(firePoint);
        }
    }

    public void EquipSlotForRemotePlayer(int slotIndex, string weaponName)
    {
        // For other players, just show the weapon visual
        if (currentWeaponObject != null)
        {
            Destroy(currentWeaponObject);
        }

        if (slotIndex == 0 || string.IsNullOrEmpty(weaponName))
        {
            // Hand or empty
            return;
        }

        // Load weapon data by name
        WeaponData weaponData = Resources.Load<WeaponData>($"Weapons/{weaponName}");
        if (weaponData != null && weaponData.weaponPrefab != null)
        {
            currentWeaponObject = Instantiate(weaponData.weaponPrefab, weaponHolder);
            currentWeaponObject.transform.localPosition = Vector3.zero;
            currentWeaponObject.transform.localRotation = Quaternion.identity;
            
            // Remote players don't need fire point updates since they don't shoot locally
        }
    }

    public InventoryItem GetCurrentItem()
    {
        return inventory[currentSlotIndex];
    }

    public int GetCurrentSlotIndex()
    {
        return currentSlotIndex;
    }

    public void AddItemToSlot(int slotIndex, InventoryItem item)
    {
        if (slotIndex < 0 || slotIndex >= maxSlots) return;
        if (slotIndex == 0) return; // Can't replace hand

        inventory[slotIndex] = item;
    }

    // Helper methods to create items
    private InventoryItem CreatePistolItem()
    {
        WeaponData pistol = Resources.Load<WeaponData>("Weapons/Pistol");
        return new InventoryItem
        {
            itemType = ItemType.Gun,
            itemName = "Pistol",
            weaponData = pistol
        };
    }

    private InventoryItem CreateRifleItem()
    {
        WeaponData rifle = Resources.Load<WeaponData>("Weapons/Rifle");
        return new InventoryItem
        {
            itemType = ItemType.Gun,
            itemName = "Rifle",
            weaponData = rifle
        };
    }
}

[System.Serializable]
public class InventoryItem
{
    public ItemType itemType;
    public string itemName;
    public WeaponData weaponData; // null for melee
}

public enum ItemType
{
    Melee,
    Gun,
    Consumable
}