using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    [Header("Ammo Configuration")]
    [SerializeField] private AmmoType ammoType = AmmoType.PistolAmmo;
    [SerializeField] private int ammoAmount = 30;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color pistolColor = new Color(0.8f, 0.8f, 0.2f);
    [SerializeField] private Color rifleColor = new Color(1f, 0.5f, 0.2f);
    [SerializeField] private Color sniperColor = new Color(0.2f, 1f, 0.2f);
    [SerializeField] private Color shotgunColor = new Color(1f, 0.2f, 0.2f);

    [Header("Bobbing Animation")]
    [SerializeField] private bool enableBobbing = true;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.3f;

    private Vector3 startPosition;
    private float bobTimer;
    private bool hasBeenPickedUp = false;

    void Start()
    {
        startPosition = transform.position;

        // Set color based on ammo type
        if (spriteRenderer != null)
        {
            spriteRenderer.color = GetColorForAmmoType(ammoType);
        }
    }

    void Update()
    {
        if (enableBobbing)
        {
            bobTimer += Time.deltaTime * bobSpeed;
            float yOffset = Mathf.Sin(bobTimer) * bobHeight;
            transform.position = startPosition + new Vector3(0, yOffset, 0);
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Prevent duplicate pickups
        if (hasBeenPickedUp) return;

        Debug.Log($"Ammo pickup trigger with: {collision.name}");

        // Check if it's a player
        if (!collision.CompareTag("Player"))
        {
            Debug.Log("Not a player, ignoring");
            return;
        }

        // IMPORTANT: Only the LOCAL player should send pickup requests
        // This prevents each client from sending duplicate requests for the same pickup
        // When Player A picks up ammo, only Player A's client sends the request
        // The server then broadcasts the pickup to all clients
        if (!collision.name.Contains("_LOCAL"))
        {
            Debug.Log("Not local player on this client, ignoring");
            return;
        }

        // Get pickup's network identity
        NetworkIdentity pickupId = GetComponent<NetworkIdentity>();
        if (pickupId == null)
        {
            Debug.LogWarning("Ammo pickup has no NetworkIdentity!");
            return;
        }

        // Mark as picked up to prevent duplicate requests
        hasBeenPickedUp = true;

        // Convert Unity AmmoType enum to server string format
        string serverAmmoType = ConvertAmmoTypeToServerString(ammoType);

        Debug.Log($"LOCAL PLAYER picked up ammo! Sending to server: ID={pickupId.GetId()}, Type={serverAmmoType}, Amount={ammoAmount}");

        // Send to server - server will:
        // 1. Add ammo to THIS player's inventory
        // 2. Send confirmation back to THIS player
        // 3. Broadcast pickup destruction to ALL players
        NetworkClient.Instance.SendAmmoPickup(
            pickupId.GetId(),
            serverAmmoType,
            ammoAmount
        );
    }

    // Convert Unity AmmoType enum to server format
    string ConvertAmmoTypeToServerString(AmmoType type)
    {
        switch (type)
        {
            case AmmoType.PistolAmmo: return "pistol";
            case AmmoType.RifleAmmo: return "rifle";
            case AmmoType.SniperAmmo: return "sniper";
            case AmmoType.ShotgunShells: return "shotgun";
            default: return "pistol";
        }
    }

    Color GetColorForAmmoType(AmmoType type)
    {
        switch (type)
        {
            case AmmoType.PistolAmmo: return pistolColor;
            case AmmoType.RifleAmmo: return rifleColor;
            case AmmoType.SniperAmmo: return sniperColor;
            case AmmoType.ShotgunShells: return shotgunColor;
            default: return Color.white;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = GetColorForAmmoType(ammoType);
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}