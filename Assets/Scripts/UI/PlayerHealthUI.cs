using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private Image healthFill;
    [SerializeField] private Image shieldFill;
    
    private PlayerHealth boundHealth;
    private bool isSearching = false;

    void Awake()
    {
        Debug.Log("🟦 PlayerHealthUI Awake called");
        Debug.Log($"🟦 HealthFill assigned: {healthFill != null}");
        Debug.Log($"🟦 ShieldFill assigned: {shieldFill != null}");
    }

    void Start()
    {
        Debug.Log("🟦 PlayerHealthUI Start called");
        StartCoroutine(FindLocalPlayer());
    }

    void Update()
    {
        if (boundHealth != null)
        {
            UpdateUI();
        }
    }

    private IEnumerator FindLocalPlayer()
    {
        isSearching = true;
        Debug.Log("🔍 Starting search for local player...");
        
        int attempts = 0;
        // Wait until NetworkClient has a ClientID
        while (string.IsNullOrEmpty(NetworkClient.ClientID))
        {
            attempts++;
            Debug.Log($"🔍 Waiting for ClientID... (attempt {attempts})");
            yield return new WaitForSeconds(0.1f);
            
            if (attempts > 100) // 10 seconds timeout
            {
                Debug.LogError("❌ TIMEOUT: NetworkClient.ClientID never set!");
                yield break;
            }
        }

        Debug.Log($"✅ ClientID found: {NetworkClient.ClientID}");

        // Now search for the local player
        attempts = 0;
        while (boundHealth == null)
        {
            attempts++;
            string localPlayerName = $"Player_{NetworkClient.ClientID}_LOCAL";
            Debug.Log($"🔍 Searching for player: {localPlayerName} (attempt {attempts})");
            
            GameObject localPlayer = GameObject.Find(localPlayerName);
            
            if (localPlayer != null)
            {
                Debug.Log($"✅ Found player GameObject: {localPlayer.name}");
                boundHealth = localPlayer.GetComponent<PlayerHealth>();
                
                if (boundHealth != null)
                {
                    Debug.Log($"✅ PlayerHealth component found!");
                    Debug.Log($"✅ Current Health: {boundHealth.CurrentHealth}/{boundHealth.maxHealth}");
                    Debug.Log($"✅ Current Shield: {boundHealth.CurrentShield}/{boundHealth.maxShield}");
                    Debug.Log($"✅ PlayerHealthUI successfully bound!");
                    isSearching = false;
                    UpdateUI();
                    yield break;
                }
                else
                {
                    Debug.LogError($"❌ Player GameObject found but no PlayerHealth component!");
                }
            }
            else
            {
                Debug.Log($"🔍 Player not found yet, retrying...");
            }
            
            yield return new WaitForSeconds(0.1f);
            
            if (attempts > 100) // 10 seconds timeout
            {
                Debug.LogError("❌ TIMEOUT: Local player never spawned!");
                yield break;
            }
        }
    }

    void UpdateUI()
    {
        if (healthFill == null)
        {
            Debug.LogError("❌ healthFill is NULL!");
            return;
        }
        
        if (shieldFill == null)
        {
            Debug.LogError("❌ shieldFill is NULL!");
            return;
        }
        
        float healthPercent = (float)boundHealth.CurrentHealth / boundHealth.maxHealth;
        float shieldPercent = (float)boundHealth.CurrentShield / boundHealth.maxShield;
        
        healthFill.fillAmount = healthPercent;
        shieldFill.fillAmount = shieldPercent;
        
        // Only log every 60 frames to avoid spam
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"🎨 UI Updated - Health: {healthPercent:P0}, Shield: {shieldPercent:P0}");
        }
    }

    public void Bind(PlayerHealth health)
    {
        boundHealth = health;
        isSearching = false;
        UpdateUI();
        Debug.Log("✅ PlayerHealthUI manually bound");
    }
}