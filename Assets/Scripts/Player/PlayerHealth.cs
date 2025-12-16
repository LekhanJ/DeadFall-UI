using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    [SerializeField] private int currentHealth;

    public bool IsDead => currentHealth <= 0;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void SetHealth(int health)
    {
        currentHealth = Mathf.Clamp(health, 0, maxHealth);

        // TODO: update HP bar / UI if you have one
        // Debug.Log($"{name} health = {currentHealth}");
    }

    public void Die()
    {
        // simple version: just disable for now
        Debug.Log($"{name} died!");
        // play animation, particle, etc.
        gameObject.SetActive(false);
    }
}
