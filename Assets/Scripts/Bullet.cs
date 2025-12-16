using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 15f;
    public float lifetime = 2f;

    Rigidbody2D rb;

    public void Init(Vector2 dir)
    {
        rb = GetComponent<Rigidbody2D>();

        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        rb.linearVelocity = dir.normalized * speed;

        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))   // avoid hitting walls, etc.
        {
            Debug.Log("Bullet hit: " + other.gameObject.name);
            Destroy(gameObject);
        }
    }
}
