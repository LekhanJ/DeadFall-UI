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

    // all players in the game (including me)
    private Dictionary<string, GameObject> serverObjects = new();

    // reference to MY player object
    private GameObject localPlayer;

    // simple send throttle
    private Vector3 lastSentPosition;
    private float stillTimer;

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

                case "updatePosition":
                    HandlePositionUpdate(data);
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

        if (localPlayer != null)
        {
            SendLocalPosition();
        }
    }

    // -------------------- handlers --------------------

    void HandleInitialState(JObject data)
    {
        ClientID = data["sessionId"]!.ToString();

        // 1. Spawn local player
        SpawnPlayer(ClientID, true);

        // 2. Spawn all other connected players
        foreach (var other in data["others"]!)
        {
            Player p = other.ToObject<Player>();
            SpawnPlayer(p.id, false);

            serverObjects[p.id].transform.position = new Vector3(
                p.position.x,
                p.position.y,
                0f
            );
        }
    }


    void HandleSpawn(JObject data)
    {
        Player p = data["player"].ToObject<Player>();

        SpawnPlayer(p.id, false);

        serverObjects[p.id].transform.position = new Vector3(
            p.position.x,
            p.position.y,
            0f
        );
    }


    void HandlePositionUpdate(JObject data)
    {
        string id = data["sessionId"]!.ToString();

        if (id == ClientID) return;
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

        var hp = obj.GetComponent<PlayerHealth>();
        if (hp == null) return;

        hp.maxHealth = maxHealth;
        hp.SetHealth(health);
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

        // If this is local, you might also:
        if (id == ClientID)
        {
            Debug.Log("You died!");
            // disable input, show game over, etc.
        }
    }

    void HandleServerSpawn(JObject data) {
        string name = data["name"]!.ToString();
        string id = data["id"]!.ToString();

        Position pos  = data["position"].ToObject<Position>();
        
        GameObject spawnedObject;

        if (!serverObjects.ContainsKey(id)) {
            ServerObjectData sod = serverSpawnables.GetObjectByName(name);
            spawnedObject = Instantiate(sod.Prefab, networkContainer);
            spawnedObject.transform.position = new Vector3(pos.x, pos.y, 0);

            if (name == "Bullet") {
                Position direction = data["direction"].ToObject<Position>();
                float rot = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                Vector3 currentRotation = new Vector3(0, 0, rot - 90);
                spawnedObject.transform.rotation = Quaternion.Euler(currentRotation);
            }

            serverObjects.Add(id, spawnedObject);
        }
    }

    void HandleServerUnspawn(JObject data) {
        string id = data["id"]!.ToString();
        DestroyImmediate(serverObjects[id]);
        serverObjects.Remove(id);
    }

    void HandleBulletMove(JObject data) {
        string id = data["id"]!.ToString();

        if (!serverObjects.TryGetValue(id, out GameObject bullet))
            return;

        Position pos = data["position"].ToObject<Position>();

        bullet.transform.position = new Vector3(pos.x, pos.y, 0);
    }


    // -------------------- sending --------------------

    void SendLocalPosition()
    {
        Vector3 pos = localPlayer.transform.position;

        // movement detection
        if (Vector3.Distance(pos, lastSentPosition) > 0.001f)
        {
            stillTimer = 0f;
            lastSentPosition = pos;
            SendPositionNow(pos);
        }
        else
        {
            stillTimer += Time.deltaTime;
            if (stillTimer >= 1f)
            {
                stillTimer = 0f;
                SendPositionNow(pos);
            }
        }
    }

    void SendPositionNow(Vector3 pos)
    {
        Position p = new Position {
            x = Mathf.Round(pos.x * 1000f) / 1000f,
            y = Mathf.Round(pos.y * 1000f) / 1000f
        };

        JObject msg = JObject.FromObject(new {
            type = "updatePosition",
            position = p
        });

        websocket.SendText(msg.ToString());
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


    public async void SendShoot(BulletData bulletData)
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
            bulletId = bulletData.id
        });

        await websocket.SendText(msg.ToString());
    }


    // -------------------- utils --------------------

    void SpawnPlayer(string id, bool isLocal)
    {
        if (serverObjects.ContainsKey(id)) return;

        GameObject obj = Instantiate(playerPrefab, networkContainer);
        obj.name = isLocal ? $"Player_{id}_LOCAL" : $"Player_{id}";

        serverObjects[id] = obj;

        if (isLocal)
        {
            localPlayer = obj;
            lastSentPosition = obj.transform.position;

            Camera.main.GetComponent<CameraController>().SetTarget(localPlayer.transform);
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
    public string id;
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
    public Position position;
    public Position direction;
}