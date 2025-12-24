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
                case "shoot":
                    HandleShoot(data);
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
                case "inventorySwitch":
                    HandleInventorySwitch(data);
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
        SpawnPlayer(ClientID, true);

        foreach (var other in data["others"]!)
        {
            Player p = other.ToObject<Player>();
            SpawnPlayer(p.sessionId, false);

            serverObjects[p.sessionId].transform.position = new Vector3(p.position.x, p.position.y, 0f);
        }
    }

    void HandleSpawn(JObject data)
    {
        Player p = data["player"].ToObject<Player>();
        SpawnPlayer(p.sessionId, false);
        serverObjects[p.sessionId].transform.position = new Vector3(p.position.x, p.position.y, 0f);
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

    void HandleShoot(JObject data)
    {
        Position pos  = data["position"].ToObject<Position>();
        Position dir  = data["direction"].ToObject<Position>();
        string weaponName = data["weaponName"]?.ToString() ?? "Pistol";

        Vector2 p = new Vector2(pos.x, pos.y);
        Vector2 d = new Vector2(dir.x, dir.y);

        GameObject bullet = Instantiate(bulletPrefab, p, Quaternion.identity, networkContainer);
        bullet.GetComponent<Bullet>().Init(d);
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
        Position pos  = data["position"].ToObject<Position>();
        
        GameObject spawnedObject;

        if (!serverObjects.ContainsKey(id)) 
        {
            ServerObjectData sod = serverSpawnables.GetObjectByName(name);
            spawnedObject = Instantiate(sod.Prefab, networkContainer);
            spawnedObject.GetComponent<NetworkIdentity>().SetId(id);
            spawnedObject.transform.position = new Vector3(pos.x, pos.y, 0);

            if (name == "Bullet") 
            {
                Position direction = data["direction"].ToObject<Position>();
                string activator = data["activator"]!.ToString();

                float rot = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                Vector3 currentRotation = new Vector3(0, 0, rot - 90);
                spawnedObject.transform.rotation = Quaternion.Euler(currentRotation);

                WhoActivatedMe whoActivatedMe = spawnedObject.GetComponent<WhoActivatedMe>();
                whoActivatedMe.SetActivator(activator);
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

    void HandleInventorySwitch(JObject data)
    {
        string id = data["sessionId"]!.ToString();
        if (id == ClientID) return;
        if (!serverObjects.TryGetValue(id, out GameObject player)) return;

        int slotIndex = data["slotIndex"]!.Value<int>();
        string weaponName = data["weaponName"]?.ToString();

        var inventory = player.GetComponent<InventorySystem>();
        if (inventory != null)
        {
            inventory.EquipSlotForRemotePlayer(slotIndex, weaponName);
        }
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

    // -------------------- sending --------------------

    public async void SendMovementInput(float horizontal, float vertical)
    {
        if (websocket.State != WebSocketState.Open) return;

        JObject msg = JObject.FromObject(new {
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

        JObject msg = JObject.FromObject(new {
            type = "aim",
            direction = d
        });

        await websocket.SendText(msg.ToString());
    }

    public async void SendShoot(BulletData bulletData, string weaponName)
    {
        if (websocket.State != WebSocketState.Open) return;

        JObject msg = JObject.FromObject(new {
            type = "shoot",
            position = new {
                x = bulletData.position.x,
                y = bulletData.position.y
            },
            direction = new {
                x = bulletData.direction.x,
                y = bulletData.direction.y
            },
            bulletId = bulletData.id,
            activator = ClientID,
            weaponName = weaponName
        });

        await websocket.SendText(msg.ToString());
    }

    public async void SendInventorySwitch(int slotIndex)
    {
        if (websocket.State != WebSocketState.Open) return;

        var inventory = localPlayer.GetComponent<InventorySystem>();
        var currentItem = inventory.GetCurrentItem();
        
        string weaponName = currentItem?.weaponData?.weaponName ?? "";

        JObject msg = JObject.FromObject(new {
            type = "inventorySwitch",
            slotIndex = slotIndex,
            weaponName = weaponName
        });

        await websocket.SendText(msg.ToString());
    }

    public async void SendMeleeAttack(string targetName, float damage)
    {
        if (websocket.State != WebSocketState.Open) return;

        string targetId = ExtractIdFromPlayerName(targetName);

        JObject msg = JObject.FromObject(new {
            type = "meleeAttack",
            targetId = targetId,
            damage = damage
        });

        await websocket.SendText(msg.ToString());
    }

    public async void SendCollisionDestroy(string id) 
    {
        if (websocket.State != WebSocketState.Open) return;

        JObject msg = JObject.FromObject(new {
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

        obj.GetComponent<NetworkIdentity>().SetId(id);

        serverObjects[id] = obj;

        if (isLocal)
        {
            localPlayer = obj;
            
            Camera.main.GetComponent<CameraController>().SetTarget(localPlayer.transform);

            PlayerHealthUI ui =
            FindFirstObjectByType<PlayerHealthUI>();

            PlayerHealth health =
                localPlayer.GetComponent<PlayerHealth>();

            ui.Bind(health);
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