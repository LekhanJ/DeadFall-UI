using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class InventoryUI : MonoBehaviour
{
    [Header("Slot Container")]
    [SerializeField] private Transform slotContainer; // Parent of all ItemSlots
    
    [Header("Visual Settings")]
    [SerializeField] private Color normalSlotColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color selectedSlotColor = new Color(1f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color emptySlotColor = new Color(0.15f, 0.15f, 0.15f, 0.6f);

    [Header("Item Icons")]
    [SerializeField] private Sprite handIcon;
    [SerializeField] private Sprite healthIcon;
    [SerializeField] private Sprite shieldIcon;
    [SerializeField] private Sprite grenadeIcon;
    [SerializeField] private Sprite defaultWeaponIcon;

    private List<InventorySlotDisplay> slots = new List<InventorySlotDisplay>();
    private InventorySystem playerInventory;
    private int maxSlots = 6;
    private int lastSelectedSlot = -1;

    void Start()
    {
        InitializeSlots();
    }

    void Update()
    {
        // Auto-find player inventory
        if (playerInventory == null && NetworkClient.ClientID != null)
        {
            GameObject localPlayer = GameObject.Find("Player_" + NetworkClient.ClientID + "_LOCAL");
            if (localPlayer != null)
            {
                playerInventory = localPlayer.GetComponent<InventorySystem>();
            }
        }

        // Update UI
        if (playerInventory != null)
        {
            RefreshAllSlots();
        }
    }

    void InitializeSlots()
    {
        // Find all ItemSlot children
        if (slotContainer == null)
        {
            slotContainer = transform;
        }

        // Get all slots from hierarchy
        for (int i = 0; i < slotContainer.childCount; i++)
        {
            Transform slotTransform = slotContainer.GetChild(i);
            
            // Create slot display wrapper
            InventorySlotDisplay slotDisplay = new InventorySlotDisplay();
            slotDisplay.Initialize(slotTransform.gameObject, i);
            
            slots.Add(slotDisplay);
        }

        maxSlots = slots.Count;
        Debug.Log($"Initialized {maxSlots} inventory display slots (visual only)");
    }

    void RefreshAllSlots()
    {
        var items = playerInventory.GetAllItems();
        int currentSlot = playerInventory.GetCurrentSlotIndex();

        // Only update if selection changed (optimization)
        bool selectionChanged = currentSlot != lastSelectedSlot;
        lastSelectedSlot = currentSlot;

        for (int i = 0; i < slots.Count; i++)
        {
            bool isSelected = i == currentSlot;

            if (i < items.Count && items[i] != null)
            {
                slots[i].UpdateDisplay(items[i], isSelected, this);
            }
            else
            {
                slots[i].ShowEmpty(isSelected, this);
            }
        }
    }

    // Get icon for item type
    public Sprite GetIconForItem(InventoryItem item)
    {
        if (item == null) return null;

        switch (item.itemType)
        {
            case "Hand":
                return handIcon;
            case "Health":
                return healthIcon;
            case "Shield":
                return shieldIcon;
            case "Grenade":
                return grenadeIcon;
            case "Weapon":
                // Try to load from WeaponData
                if (!string.IsNullOrEmpty(item.weaponName))
                {
                    WeaponData weaponData = Resources.Load<WeaponData>($"Weapons/{item.weaponName}");
                    if (weaponData != null && weaponData.weaponIcon != null)
                    {
                        return weaponData.weaponIcon;
                    }
                }
                return defaultWeaponIcon;
            default:
                return defaultWeaponIcon;
        }
    }

    public Color GetNormalColor() => normalSlotColor;
    public Color GetSelectedColor() => selectedSlotColor;
    public Color GetEmptyColor() => emptySlotColor;
}

[System.Serializable]
public class InventorySlotDisplay
{
    private GameObject slotObject;
    private int slotIndex;

    // UI Components (auto-found)
    private Image backgroundImage;
    private Image itemIconImage;
    private TextMeshProUGUI amountText;
    private TextMeshProUGUI slotNumberText;
    private GameObject itemImageObject;

    private InventoryItem currentItem;
    private bool isCurrentlySelected;

    public void Initialize(GameObject slot, int index)
    {
        slotObject = slot;
        slotIndex = index;

        // Remove any interactive components
        Button button = slot.GetComponent<Button>();
        if (button != null)
        {
            Object.Destroy(button);
        }

        EventTrigger eventTrigger = slot.GetComponent<EventTrigger>();
        if (eventTrigger != null)
        {
            Object.Destroy(eventTrigger);
        }

        // Find or add background image
        backgroundImage = slot.GetComponent<Image>();
        if (backgroundImage == null)
        {
            backgroundImage = slot.AddComponent<Image>();
        }

        // Make non-interactive
        backgroundImage.raycastTarget = false;

        // Find "Item Image" child
        Transform itemImageTransform = slot.transform.Find("Item Image");
        if (itemImageTransform != null)
        {
            itemImageObject = itemImageTransform.gameObject;
            itemIconImage = itemImageObject.GetComponent<Image>();
            
            if (itemIconImage != null)
            {
                itemIconImage.raycastTarget = false;
            }
        }
        else
        {
            Debug.LogWarning($"Slot {index + 1} missing 'Item Image' child!");
        }

        // Find "AmountText" child
        Transform amountTransform = slot.transform.Find("AmountText");
        if (amountTransform != null)
        {
            amountText = amountTransform.GetComponent<TextMeshProUGUI>();
            if (amountText != null)
            {
                amountText.raycastTarget = false;
            }
        }

        // Find or create "SlotNumber" text
        Transform slotNumberTransform = slot.transform.Find("SlotNumber");
        if (slotNumberTransform != null)
        {
            slotNumberText = slotNumberTransform.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            // Optional: Auto-create slot number display
            GameObject slotNumberObj = new GameObject("SlotNumber");
            slotNumberObj.transform.SetParent(slot.transform, false);
            
            slotNumberText = slotNumberObj.AddComponent<TextMeshProUGUI>();
            slotNumberText.text = (index + 1).ToString();
            slotNumberText.fontSize = 14;
            slotNumberText.color = new Color(1f, 1f, 1f, 0.6f);
            slotNumberText.alignment = TextAlignmentOptions.TopLeft;
            slotNumberText.raycastTarget = false;
            
            RectTransform rt = slotNumberObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(5, -5);
            rt.sizeDelta = new Vector2(30, 30);
        }
    }

    public void UpdateDisplay(InventoryItem item, bool selected, InventoryUI controller)
    {
        currentItem = item;
        isCurrentlySelected = selected;

        // Show item icon
        if (itemIconImage != null)
        {
            itemIconImage.sprite = controller.GetIconForItem(item);
            itemIconImage.enabled = true;
            
            if (itemImageObject != null)
            {
                itemImageObject.SetActive(true);
            }
        }

        // Show amount for consumables
        if (amountText != null)
        {
            if (item.amount > 0)
            {
                amountText.text = "x" + item.amount;
                amountText.gameObject.SetActive(true);
            }
            else
            {
                amountText.gameObject.SetActive(false);
            }
        }

        // Update background color
        UpdateBackgroundColor(controller, false);
    }

    public void ShowEmpty(bool selected, InventoryUI controller)
    {
        currentItem = null;
        isCurrentlySelected = selected;

        // Hide item icon
        if (itemIconImage != null)
        {
            itemIconImage.enabled = false;
            
            if (itemImageObject != null)
            {
                itemImageObject.SetActive(false);
            }
        }

        // Hide amount text
        if (amountText != null)
        {
            amountText.gameObject.SetActive(false);
        }

        // Update background color
        UpdateBackgroundColor(controller, true);
    }

    void UpdateBackgroundColor(InventoryUI controller, bool isEmpty)
    {
        if (backgroundImage == null) return;

        if (isCurrentlySelected)
        {
            backgroundImage.color = controller.GetSelectedColor();
        }
        else if (isEmpty)
        {
            backgroundImage.color = controller.GetEmptyColor();
        }
        else
        {
            backgroundImage.color = controller.GetNormalColor();
        }
    }
}