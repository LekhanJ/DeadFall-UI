using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;
using Newtonsoft.Json.Linq;

public class NetworkClient : MonoBehaviour
{
    [Header("Connection")]
    public string serverUrl = "ws://localhost:3000";
    private WebSocket websocket;

    public static string ClientID { get; private set; }
    public static NetworkClient Instance { get; private set; }

    [Header("Scene")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform networkContainer;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private ServerObjects serverSpawnables;

    private Dictionary<string, GameObject> serverObjects = new();
    private GameObject localPlayer;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    async void Start()
    {
        websocket = new WebSocket(serverUrl);

        websocket.OnOpen += () =>
        {
            Debug.Log("✅ Connected to server");
        };

        websocket.OnMessage += (bytes) =>
        {
            string json = System.Text.Encoding.UTF8.GetString(bytes);
            JObject data = JObject.Parse(json);

            string type = data["type"]!.ToString();

            switch (type)
            {
                case "initialState":
                    HandleInitialState(data);
                    break;
                case "spawn":
                    HandleSpawn(data);
                    break;
                case "serverPositionUpdate":
                    HandleServerPositionUpdate(data);
                    break;
                case "player_left":
                    HandlePlayerLeft(data);
                    break;
                case "aim":
                    HandleAimUpdate(data);
                    break;
                case "healthUpdate":
                    HandleHealthUpdate(data);
                    break;
                case "playerKilled":
                    HandlePlayerKilled(data);
                    break;
                case "serverSpawn":
                    HandleServerSpawn(data);
                    break;
                case "serverUnspawn":
                    HandleServerUnspawn(data);
                    break;
                case "bulletMove":
                    HandleBulletMove(data);
                    break;
                case "grenadeMove":
                    HandleGrenadeMove(data);
                    break;
                case "grenadeExplode":
                    HandleGrenadeExplode(data);
                    break;
                case "inventoryUpdate":
                    HandleInventoryUpdate(data);
                    break;
                case "weaponStateUpdate":
                    HandleWeaponStateUpdate(data);
                    break;
                case "fullPlayerState":
                    HandleFullPlayerState(data);
                    break;
                case "reloadStarted":
                    HandleReloadStarted(data);
                    break;
                case "reloadCompleted":
                    HandleReloadCompleted(data);
                    break;
                case "shootRejected":
                    HandleShootRejected(data);
                    break;
                case "ammoPickupConfirmed":
                    HandleAmmoPickupConfirmed(data);
                    break;
                case "meleeAttack":
                    HandleMeleeAttack(data);
                    break;
            }
        };

        websocket.OnClose += (_) =>
        {
            Debug.Log("❌ Disconnected from server");
        };

        await websocket.Connect();
    }

    void Update()
    {
        #if !UNITY_WEBGL || UNITY_EDITOR
            websocket?.DispatchMessageQueue();
        #endif
    }

    // -------------------- handlers --------------------

    void HandleInitialState(JObject data)
    {
        ClientID = data["sessionId"]!.ToString();
        Debug.Log($"🟢 CLIENT ID SET: {ClientID}");
        
        SpawnPlayer(ClientID, true);

        // Set local player inventory
        if (data["inventory"] != null)
        {
            ApplyInventoryData(ClientID, data["inventory"]);
        }

        // Set weapon state
        if (data["weaponState"] != null)
        {
            ApplyWeaponState(ClientID, data["weaponState"]);
        }

        // Spawn other players
        foreach (var other in data["others"]!)
        {
            Player p = other.ToObject<Player>();
            SpawnPlayer(p.sessionId, false);

            serverObjects[p.sessionId].transform.position = new Vector3(p.position.x, p.position.y, 0f);

            if (other["inventory"] != null)
            {
                ApplyInventoryData(p.sessionId, other["inventory"]);
            }
        }
    }

    void HandleSpawn(JObject data)
    {
        Player p = data["player"].ToObject<Player>();
        SpawnPlayer(p.sessionId, false);
        serverObjects[p.sessionId].transform.position = new Vector3(p.position.x, p.position.y, 0f);

        if (data["inventory"] != null)
        {
            ApplyInventoryData(p.sessionId, data["inventory"]);
        }
    }

    void HandleServerPositionUpdate(JObject data)
    {
        string id = data["sessionId"]!.ToString();
        if (!serverObjects.TryGetValue(id, out GameObject obj)) return;

        Position pos = data["position"].ToObject<Position>();
        obj.transform.position = new Vector3(pos.x, pos.y, obj.transform.position.z);
    }

    void HandlePlayerLeft(JObject data)
    {
        string id = data["sessionId"]!.ToString();
        if (serverObjects.TryGetValue(id, out GameObject obj))
        {
            Destroy(obj);
            serverObjects.Remove(id);
        }
    }

    void HandleAimUpdate(JObject data)
    {
        string id = data["sessionId"]!.ToString();
        if (!serverObjects.TryGetValue(id, out GameObject player)) return;

        Position dir = data["direction"].ToObject<Position>();
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

        player.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void HandleHealthUpdate(JObject data)
    {
        string id = data["sessionId"]!.ToString();
        if (!serverObjects.TryGetValue(id, out GameObject obj)) return;

        int health = data["health"]!.Value<int>();
        int maxHealth = data["maxHealth"]!.Value<int>();
        int shield = data["shield"].Value<int>();
        int maxShield = data["maxShield"].Value<int>();

        var hp = obj.GetComponent<PlayerHealth>();
        if (hp == null) return;

        hp.maxHealth = maxHealth;
        hp.maxShield = maxShield;
        hp.SetHealth(health, shield);
    }

    void HandlePlayerKilled(JObject data)
    {
        string id = data["sessionId"]!.ToString();
        if (!serverObjects.TryGetValue(id, out GameObject obj)) return;

        var hp = obj.GetComponent<PlayerHealth>();
        if (hp != null)
        {
            hp.Die();
        }

        if (id == ClientID)
        {
            Debug.Log("You died!");
        }
    }

    void HandleServerSpawn(JObject data)
    {
        string name = data["name"]!.ToString();
        string id = data["id"]!.ToString();
        Position pos = data["position"].ToObject<Position>();
        
        GameObject spawnedObject;

        if (!serverObjects.ContainsKey(id))
        {
            ServerObjectData sod = serverSpawnables.GetObjectByName(name);
            spawnedObject = Instantiate(sod.Prefab, networkContainer);
            spawnedObject.GetComponent<NetworkIdentity>().SetId(id);
            spawnedObject.transform.position = new Vector3(pos.x, pos.y, 0);

            if (name == "Bullet" || name == "Grenade")
            {
                Position direction = data["direction"].ToObject<Position>();
                string activator = data["activator"]!.ToString();

                float rot = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                Vector3 currentRotation = new Vector3(0, 0, rot - 90);
                spawnedObject.transform.rotation = Quaternion.Euler(currentRotation);

                WhoActivatedMe whoActivatedMe = spawnedObject.GetComponent<WhoActivatedMe>();
                if (whoActivatedMe != null)
                {
                    whoActivatedMe.SetActivator(activator);
                }
            }

            serverObjects.Add(id, spawnedObject);
        }
    }

    void HandleServerUnspawn(JObject data)
    {
        string id = data["id"]!.ToString();
        if (serverObjects.ContainsKey(id))
        {
            DestroyImmediate(serverObjects[id]);
            serverObjects.Remove(id);
        }
    }

    void HandleBulletMove(JObject data)
    {
        string id = data["id"]!.ToString();
        if (!serverObjects.TryGetValue(id, out GameObject bullet)) return;

        Position pos = data["position"].ToObject<Position>();
        bullet.transform.position = new Vector3(pos.x, pos.y, 0);
    }

    void HandleGrenadeMove(JObject data)
    {
        string id = data["id"]!.ToString();
        if (!serverObjects.TryGetValue(id, out GameObject grenade)) return;

        Position pos = data["position"].ToObject<Position>();
        grenade.transform.position = new Vector3(pos.x, pos.y, 0);
    }

    void HandleGrenadeExplode(JObject data)
    {
        string id = data["id"]!.ToString();
        Position pos = data["position"].ToObject<Position>();
        float radius = data["radius"]!.Value<float>();

        Vector2 explosionPos = new Vector2(pos.x, pos.y);

        if (serverObjects.TryGetValue(id, out GameObject grenade))
        {
            var grenadeVisual = grenade.GetComponent<GrenadeVisual>();
            if (grenadeVisual != null)
            {
                grenadeVisual.Explode(explosionPos, radius);
            }
            else
            {
                Destroy(grenade);
            }

            serverObjects.Remove(id);
        }

        Debug.Log($"Grenade exploded at {explosionPos} with radius {radius}");
    }

    void HandleInventoryUpdate(JObject data)
    {
        string id = data["sessionId"]!.ToString();
        if (!serverObjects.TryGetValue(id, out GameObject player)) return;

        int slotIndex = data["slotIndex"]!.Value<int>();
        
        InventoryItem item = null;
        if (data["item"] != null && data["item"].Type != JTokenType.Null)
        {
            item = data["item"].ToObject<InventoryItem>();
        }

        var inventory = player.GetComponent<InventorySystem>();
        if (inventory != null)
        {
            inventory.UpdateInventoryFromServer(slotIndex, item);
        }
    }

    void HandleWeaponStateUpdate(JObject data)
    {
        if (localPlayer == null) return;

        var weaponStateData = data["weaponState"];
        var ammoData = data["ammo"];
        
        // Update weapon state FIRST (includes current/magazine ammo)
        if (weaponStateData != null && weaponStateData.Type != JTokenType.Null)
        {
            ApplyWeaponState(NetworkClient.ClientID, weaponStateData);
        }

        // Update reserve ammo SECOND
        if (ammoData != null && ammoData.Type != JTokenType.Null)
        {
            UpdateAmmoFromServer(ammoData);
        }

        Debug.Log("Weapon state updated from server");
    }

    void HandleFullPlayerState(JObject data)
    {
        if (localPlayer == null) return;

        var inventoryData = data["inventory"];
        var weaponStateData = data["weaponState"];
        var ammoData = data["ammo"];

        if (inventoryData != null)
        {
            ApplyInventoryData(ClientID, inventoryData);
        }

        if (weaponStateData != null && weaponStateData.Type != JTokenType.Null)
        {
            ApplyWeaponState(ClientID, weaponStateData);
        }

        if (ammoData != null && ammoData.Type != JTokenType.Null)
        {
            UpdateAmmoFromServer(ammoData);
        }
    }

    void HandleReloadStarted(JObject data)
    {
        if (localPlayer == null) return;

        string weaponName = data["weaponName"]!.ToString();
        float reloadTime = data["reloadTime"]!.Value<float>();

        var weaponController = localPlayer.GetComponent<WeaponController>();
        if (weaponController != null)
        {
            weaponController.OnReloadStarted(weaponName, reloadTime);
        }
    }

    void HandleReloadCompleted(JObject data)
    {
        if (localPlayer == null) return;

        var weaponStateData = data["weaponState"];
        
        if (weaponStateData != null && weaponStateData.Type != JTokenType.Null)
        {
            ApplyWeaponState(ClientID, weaponStateData);
        }

        var weaponController = localPlayer.GetComponent<WeaponController>();
        if (weaponController != null)
        {
            weaponController.OnReloadCompleted();
        }
    }

    void HandleShootRejected(JObject data)
    {
        string reason = data["reason"]!.ToString();
        Debug.LogWarning($"❌ Shoot rejected by server: {reason}");
        
        // Revert optimistic prediction if needed
        if (localPlayer != null)
        {
            WeaponController weaponController = localPlayer.GetComponent<WeaponController>();
            if (weaponController != null)
            {
                // Server will send the correct state, so just log for now
                Debug.Log("Waiting for server to send correct weapon state...");
            }
        }
    }
    
    void HandleAmmoPickupConfirmed(JObject data)
    {
        string pickupId = data["pickupId"]!.ToString();
        string ammoType = data["ammoType"]!.ToString();
        int amount = data["amount"]!.Value<int>();

        Debug.Log($"✅ Ammo pickup confirmed: {amount} {ammoType} ammo");

        // The weapon state update will come separately from the server
        // with the updated ammo counts, so we don't need to manually update here
        
        // Find and destroy the pickup object (visual only)
        GameObject pickup = FindObjectByNetworkId(pickupId);
        if (pickup != null)
        {
            Debug.Log($"Destroying pickup visual: {pickupId}");
            Destroy(pickup);
        }
    }

    // Helper method - add this to NetworkClient class
    AmmoType ConvertServerStringToAmmoType(string serverType)
    {
        switch (serverType.ToLower())
        {
            case "pistol": return AmmoType.PistolAmmo;
            case "rifle": return AmmoType.RifleAmmo;
            case "sniper": return AmmoType.SniperAmmo;
            case "shotgun": return AmmoType.ShotgunShells;
            default: return AmmoType.None;
        }
    }

    GameObject FindObjectByNetworkId(string id)
    {
        NetworkIdentity[] all = FindObjectsByType<NetworkIdentity>(FindObjectsSortMode.None);
        foreach (var ni in all)
        {
            if (ni.GetId() == id)
                return ni.gameObject;
        }
        return null;
    }


    void HandleMeleeAttack(JObject data)
    {
        string attackerId = data["attackerId"]!.ToString();
        string targetId = data["targetId"]!.ToString();
        float damage = data["damage"]!.Value<float>();

        Debug.Log($"Player {attackerId} punched {targetId} for {damage} damage");

        if (targetId == ClientID)
        {
            Debug.Log("You got punched!");
        }
    }

    void ApplyInventoryData(string playerId, JToken inventoryData)
    {
        if (!serverObjects.TryGetValue(playerId, out GameObject player)) return;

        var slots = inventoryData["slots"].ToObject<InventoryItem[]>();
        int currentIndex = inventoryData["currentSlotIndex"].Value<int>();

        var inventory = player.GetComponent<InventorySystem>();
        if (inventory != null)
        {
            inventory.SetFullInventory(slots, currentIndex);
        }
    }

    void ApplyWeaponState(string playerId, JToken weaponStateData)
    {
        if (!serverObjects.TryGetValue(playerId, out GameObject player)) return;

        var weaponState = weaponStateData.ToObject<WeaponState>();

        var weaponController = player.GetComponent<WeaponController>();
        if (weaponController != null)
        {
            weaponController.UpdateWeaponState(weaponState);
            Debug.Log($"Applied weapon state: {weaponState.weaponName} - {weaponState.currentAmmo}/{weaponState.magazineCapacity}");
        }
    }

    void UpdateAmmoFromServer(JToken ammoData)
    {
        int pistol = ammoData["pistol"]?.Value<int>() ?? 0;
        int rifle = ammoData["rifle"]?.Value<int>() ?? 0;
        int sniper = ammoData["sniper"]?.Value<int>() ?? 0;
        int shotgun = ammoData["shotgun"]?.Value<int>() ?? 0;

        Debug.Log($"Updating ammo from server: P={pistol}, R={rifle}, S={sniper}, SG={shotgun}");

        // Update local AmmoManager
        if (localPlayer != null)
        {
            AmmoManager ammoManager = localPlayer.GetComponent<AmmoManager>();
            if (ammoManager != null)
            {
                ammoManager.SetAmmo(AmmoType.PistolAmmo, pistol);
                ammoManager.SetAmmo(AmmoType.RifleAmmo, rifle);
                ammoManager.SetAmmo(AmmoType.SniperAmmo, sniper);
                ammoManager.SetAmmo(AmmoType.ShotgunShells, shotgun);
                // This will automatically trigger NotifyAmmoChanged() which updates the UI
            }
        }
    }

    // -------------------- sending --------------------

    public async void SendMovementInput(float horizontal, float vertical)
    {
        if (websocket.State != WebSocketState.Open) return;

        JObject msg = JObject.FromObject(new
        {
            type = "moveInput",
            horizontal = horizontal,
            vertical = vertical
        });

        await websocket.SendText(msg.ToString());
    }

    public async void SendAim(Vector2 dir)
    {
        if (websocket.State != WebSocketState.Open) return;

        Position d = new Position { x = dir.x, y = dir.y };

        JObject msg = JObject.FromObject(new
        {
            type = "aim",
            direction = d
        });

        await websocket.SendText(msg.ToString());
    }

    public async void SendShootRequest(Vector3 position, Vector2 direction)
    {
        if (websocket.State != WebSocketState.Open) return;

        JObject msg = JObject.FromObject(new
        {
            type = "shootRequest",
            position = new
            {
                x = position.x,
                y = position.y
            },
            direction = new
            {
                x = direction.x,
                y = direction.y
            }
        });

        await websocket.SendText(msg.ToString());
    }

    public async void SendReloadRequest()
    {
        if (websocket.State != WebSocketState.Open) return;

        JObject msg = JObject.FromObject(new
        {
            type = "reloadRequest"
        });

        await websocket.SendText(msg.ToString());
    }

    public async void SendInventorySwitch(int slotIndex)
    {
        if (websocket.State != WebSocketState.Open) return;

        JObject msg = JObject.FromObject(new
        {
            type = "inventorySwitch",
            slotIndex = slotIndex
        });

        await websocket.SendText(msg.ToString());
    }

    public async void SendUseItem(int slotIndex)
    {
        if (websocket.State != WebSocketState.Open) return;

        JObject msg = JObject.FromObject(new
        {
            type = "useItem",
            slotIndex = slotIndex
        });

        await websocket.SendText(msg.ToString());
    }

    public async void SendThrowGrenade(Vector3 position, Vector2 direction)
    {
        if (websocket.State != WebSocketState.Open) return;

        JObject msg = JObject.FromObject(new
        {
            type = "throwGrenade",
            position = new { x = position.x, y = position.y },
            direction = new { x = direction.x, y = direction.y }
        });

        await websocket.SendText(msg.ToString());
    }

    public async void SendMeleeAttack(string targetName, float damage)
    {
        if (websocket.State != WebSocketState.Open) return;

        string targetId = ExtractIdFromPlayerName(targetName);

        JObject msg = JObject.FromObject(new
        {
            type = "meleeAttack",
            targetId = targetId,
            damage = damage
        });

        await websocket.SendText(msg.ToString());
    }

    public async void SendAmmoPickup(string pickupId, string ammoType, int amount)
    {
        if (websocket.State != WebSocketState.Open) return;

        JObject msg = JObject.FromObject(new
        {
            type = "ammoPickup",
            pickupId = pickupId,
            ammoType = ammoType,
            amount = amount
        });

        await websocket.SendText(msg.ToString());
    }

    public async void SendCollisionDestroy(string id)
    {
        if (websocket.State != WebSocketState.Open) return;

        JObject msg = JObject.FromObject(new
        {
            type = "bulletCollide",
            id = id
        });

        await websocket.SendText(msg.ToString());
    }

    string ExtractIdFromPlayerName(string playerName)
    {
        string[] parts = playerName.Split('_');
        if (parts.Length >= 2)
        {
            return parts[1];
        }
        return "";
    }

    // -------------------- utils --------------------

    void SpawnPlayer(string id, bool isLocal)
    {
        if (serverObjects.ContainsKey(id)) return;

        GameObject obj = Instantiate(playerPrefab, networkContainer);
        obj.name = isLocal ? $"Player_{id}_LOCAL" : $"Player_{id}";
        
        Debug.Log($"🟢 Spawned player: {obj.name}");

        obj.GetComponent<NetworkIdentity>().SetId(id);
        serverObjects[id] = obj;

        if (isLocal)
        {
            localPlayer = obj;
            Debug.Log($"🟢 Local player set: {localPlayer.name}");

            Camera.main
                .GetComponent<CameraController>()
                .SetTarget(localPlayer.transform);

            Debug.Log($"✅ Local player fully initialized: {obj.name}");
        }
    }

    private async void OnApplicationQuit()
    {
        if (websocket != null)
        {
            await websocket.Close();
        }
    }
}

[System.Serializable]
public class Player {
    public string sessionId;
    public Position position;
}

[System.Serializable]
public class Position {
    public float x;
    public float y;
}

[System.Serializable]
public class PlayerRotation {
    public float rotation;
}

[System.Serializable]
public class BulletData {
    public string id;
    public string activator;
    public Position position;
    public Position direction;
}

[System.Serializable]
public class IDData {
    public string id;
}