using UnityEngine;

public class GrenadeVisual : MonoBehaviour
{
    [Header("Visual Effects")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float rotationSpeed = 360f; // Degrees per second
    
    private SpriteRenderer spriteRenderer;
    private TrailRenderer trailRenderer;
    private bool hasExploded = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        trailRenderer = GetComponent<TrailRenderer>();
    }

    void Update()
    {
        if (!hasExploded)
        {
            // Rotate grenade as it flies through the air
            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }
    }

    // Called from NetworkClient when grenade explodes
    public void Explode(Vector2 position, float radius)
    {
        if (hasExploded) return;
        
        hasExploded = true;

        // Spawn explosion effect
        if (explosionPrefab != null)
        {
            GameObject explosion = Instantiate(explosionPrefab, position, Quaternion.identity);
            
            // Scale explosion based on radius
            explosion.transform.localScale = Vector3.one * radius;
            
            // Auto-destroy explosion after animation
            Destroy(explosion, 2f);
        }
        else
        {
            // Fallback: Create simple explosion without prefab
            CreateSimpleExplosion(position, radius);
        }

        // Optional: Play explosion sound
        // AudioSource.PlayClipAtPoint(explosionSound, position);

        // Optional: Camera shake
        // CameraShake.Instance?.Shake(0.3f, 0.2f);

        // Destroy grenade visual
        Destroy(gameObject);
    }

    void CreateSimpleExplosion(Vector2 position, float radius)
    {
        // Create a simple visual explosion using basic Unity objects
        GameObject explosion = new GameObject("SimpleExplosion");
        explosion.transform.position = position;

        // Add sprite renderer with circle
        SpriteRenderer sr = explosion.AddComponent<SpriteRenderer>();
        
        // Create a simple circle texture
        Texture2D circleTexture = new Texture2D(64, 64);
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(32, 32));
                float alpha = Mathf.Clamp01(1f - distance / 32f);
                circleTexture.SetPixel(x, y, new Color(1f, 0.5f, 0f, alpha)); // Orange
            }
        }
        circleTexture.Apply();

        Sprite circleSprite = Sprite.Create(circleTexture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        sr.sprite = circleSprite;
        sr.sortingOrder = 100;
        
        explosion.transform.localScale = Vector3.one * radius;

        // Add simple fade out script
        var fader = explosion.AddComponent<SimpleFader>();
        fader.fadeSpeed = 2f;
        
        Destroy(explosion, 1f);
    }
}

// Simple script to fade out explosion
public class SimpleFader : MonoBehaviour
{
    public float fadeSpeed = 2f;
    private SpriteRenderer sr;
    private float alpha = 1f;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        alpha -= fadeSpeed * Time.deltaTime;
        alpha = Mathf.Max(0, alpha);
        
        if (sr != null)
        {
            Color color = sr.color;
            color.a = alpha;
            sr.color = color;
        }
    }
}