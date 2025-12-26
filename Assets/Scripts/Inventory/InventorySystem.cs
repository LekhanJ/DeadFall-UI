using UnityEngine;
using System.Collections.Generic;

public class InventorySystem : MonoBehaviour
{
    [Header("Inventory Settings")]
    public int maxSlots = 6;
    private List<InventoryItem> inventory = new List<InventoryItem>();
    private int currentSlotIndex = 0;

    [Header("Weapon Holder")]
    public Transform weaponHolder;

    private GameObject currentItemObject;
    private bool isLocal;

    [Header("Item Visuals")]
    [SerializeField] private GameObject handPrefab;
    [SerializeField] private GameObject healthPackVisual;
    [SerializeField] private GameObject shieldPackVisual;
    [SerializeField] private GameObject grenadeVisual;

    [Header("Hand Sprites")]
    [SerializeField] private SpriteRenderer leftHand;
    [SerializeField] private SpriteRenderer rightHand;

    void Awake()
    {
        // Initialize with empty slots
        for (int i = 0; i < maxSlots; i++)
        {
            inventory.Add(null);
        }
    }

    void Start()
    {
        isLocal = gameObject.name.Contains("_LOCAL");
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
                RequestSlotSwitch(i);
            }
        }

        // Mouse scroll wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            int nextSlot = (currentSlotIndex - 1 + maxSlots) % maxSlots;
            RequestSlotSwitch(nextSlot);
        }
        else if (scroll < 0f)
        {
            int nextSlot = (currentSlotIndex + 1) % maxSlots;
            RequestSlotSwitch(nextSlot);
        }

        // Use consumables with E key
        if (Input.GetKeyDown(KeyCode.E))
        {
            InventoryItem item = GetCurrentItem();
            if (item != null && (item.itemType == "Health" || item.itemType == "Shield"))
            {
                NetworkClient.Instance.SendUseItem(currentSlotIndex);
            }
        }

        // Throw grenade with G key (only if holding grenade)
        if (Input.GetKeyDown(KeyCode.G))
        {
            InventoryItem item = GetCurrentItem();
            if (item != null && item.itemType == "Grenade" && item.amount > 0)
            {
                // Throw from player position towards mouse
                Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector2 throwDirection = (mouseWorld - transform.position).normalized;
                
                // Spawn position slightly in front of player
                Vector2 spawnPos = (Vector2)transform.position + throwDirection * 0.5f;
                
                NetworkClient.Instance.SendThrowGrenade(spawnPos, throwDirection);
            }
        }
    }

    void RequestSlotSwitch(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxSlots) return;
        if (inventory[slotIndex] == null) return;

        // Send to server
        NetworkClient.Instance.SendInventorySwitch(slotIndex);
    }

    // Called by NetworkClient when server confirms slot switch
    public void UpdateInventoryFromServer(int slotIndex, InventoryItem item)
    {
        currentSlotIndex = slotIndex;
        EquipSlot(slotIndex, item);
    }

    // Called by NetworkClient when full inventory is received
    public void SetFullInventory(InventoryItem[] items, int currentIndex)
    {
        inventory.Clear();
        
        for (int i = 0; i < maxSlots; i++)
        {
            if (i < items.Length)
            {
                inventory.Add(items[i]);
            }
            else
            {
                inventory.Add(null);
            }
        }

        currentSlotIndex = currentIndex;
        EquipSlot(currentSlotIndex, items[currentIndex]);
    }

    void EquipSlot(int slotIndex, InventoryItem item)
    {
        // Destroy current item visual
        if (currentItemObject != null)
        {
            Destroy(currentItemObject);
        }

        if (item == null) {
            leftHand.enabled = true;
            rightHand.enabled = true;
            return;
        }

        // Spawn item visual based on type
        switch (item.itemType)
        {
            case "Weapon":
                if (!string.IsNullOrEmpty(item.weaponName))
                {
                    SpawnWeapon(item.weaponName);
                }
                break;

            case "Hand":
                SpawnHand();
                break;

            case "Health":
                SpawnHealth();
                break;

            case "Shield":
                SpawnShield();
                break;

            case "Grenade":
                SpawnGrenade();
                break;
        }

        Debug.Log($"Equipped: {item.itemType} - {item.itemName}" + 
                  (item.amount > 0 ? $" x{item.amount}" : ""));
    }

    void SpawnWeapon(string weaponName)
    {
        WeaponData weaponData = Resources.Load<WeaponData>($"Weapons/{weaponName}");
        if (weaponData != null && weaponData.weaponPrefab != null)
        {   
            leftHand.enabled = false;
            rightHand.enabled = false;
            currentItemObject = Instantiate(weaponData.weaponPrefab, weaponHolder);
            currentItemObject.transform.localPosition = Vector3.zero;
            currentItemObject.transform.localRotation = Quaternion.identity;

            if (isLocal)
            {
                UpdateFirePointReference();
            }
        }
        else
        {
            Debug.LogWarning($"Weapon '{weaponName}' not found in Resources/Weapons/");
        }
    }

    void SpawnHand()
    {
        if (handPrefab != null)
        {
            leftHand.enabled = true;
            rightHand.enabled = true;
            currentItemObject = Instantiate(handPrefab, weaponHolder);
            currentItemObject.transform.localPosition = Vector3.zero;
            currentItemObject.transform.localRotation = Quaternion.identity;
        }

        if (isLocal)
        {
            WeaponController weaponController = GetComponent<WeaponController>();
            if (weaponController != null)
            {
                weaponController.SetFirePoint(null); // Melee doesn't need fire point
            }
        }
    }

    void SpawnHealth()
    {
        // Optional: Show visual of health pack in hand
        if (healthPackVisual != null)
        {
            leftHand.enabled = false;
            rightHand.enabled = false;
            currentItemObject = Instantiate(healthPackVisual, weaponHolder);
            Debug.Log("Current Item Object is : " + currentItemObject);
            currentItemObject.transform.localPosition = Vector3.zero;
            currentItemObject.transform.localRotation = Quaternion.identity;
        }

        if (isLocal)
        {
            WeaponController weaponController = GetComponent<WeaponController>();
            if (weaponController != null)
            {
                weaponController.SetFirePoint(null); // Consumables don't shoot
            }
        }

        // Optional: Show UI hint "Press E to use"
        Debug.Log("Health pack equipped - Press E to use");
    }

    void SpawnShield()
    {
        // Optional: Show visual of shield pack in hand
        if (shieldPackVisual != null)
        {
            leftHand.enabled = false;
            rightHand.enabled = false;
            currentItemObject = Instantiate(shieldPackVisual, weaponHolder);
            Debug.Log("Current Item Object is : " + currentItemObject);
            currentItemObject.transform.localPosition = Vector3.zero;
            currentItemObject.transform.localRotation = Quaternion.identity;
        }

        if (isLocal)
        {
            WeaponController weaponController = GetComponent<WeaponController>();
            if (weaponController != null)
            {
                weaponController.SetFirePoint(null); // Consumables don't shoot
            }
        }

        // Optional: Show UI hint "Press E to use"
        Debug.Log("Shield pack equipped - Press E to use");
    }

    void SpawnGrenade()
    {
        // Optional: Show visual of grenade in hand
        if (grenadeVisual != null)
        {
            leftHand.enabled = false;
            rightHand.enabled = false;
            currentItemObject = Instantiate(grenadeVisual, weaponHolder);
            currentItemObject.transform.localPosition = Vector3.zero;
            currentItemObject.transform.localRotation = Quaternion.identity;
        }

        if (isLocal)
        {
            WeaponController weaponController = GetComponent<WeaponController>();
            if (weaponController != null)
            {
                weaponController.SetFirePoint(null); // Grenades are thrown, not shot
            }
        }

        // Optional: Show UI hint "Press G to throw"
        InventoryItem item = GetCurrentItem();
        if (item != null)
        {
            Debug.Log($"Grenade equipped - Press G to throw ({item.amount} remaining)");
        }
    }

    void UpdateFirePointReference()
    {
        if (currentItemObject == null) return;

        Transform firePoint = currentItemObject.transform.Find("FirePoint");
        
        if (firePoint == null)
        {
            Debug.LogWarning($"Weapon {currentItemObject.name} is missing a 'FirePoint' child!");
        }

        WeaponController weaponController = GetComponent<WeaponController>();
        if (weaponController != null)
        {
            weaponController.SetFirePoint(firePoint);
        }
    }

    public InventoryItem GetCurrentItem()
    {
        if (currentSlotIndex < 0 || currentSlotIndex >= inventory.Count)
            return null;
        return inventory[currentSlotIndex];
    }

    public int GetCurrentSlotIndex()
    {
        return currentSlotIndex;
    }

    public List<InventoryItem> GetAllItems()
    {
        return inventory;
    }
}

[System.Serializable]
public class InventoryItem
{
    public string itemType;      // "Hand", "Weapon", "Health", "Shield", "Grenade"
    public string itemName;
    public string weaponName;    // For weapons
    public int amount;           // For consumables
}