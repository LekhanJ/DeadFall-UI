using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int maxShield = 100;

    public int CurrentHealth { get; private set; }
    public int CurrentShield { get; private set; }

    void Awake()
    {
        CurrentHealth = maxHealth;
        CurrentShield = maxShield;
    }

    public void SetHealth(int health, int shield)
    {
        CurrentHealth = Mathf.Clamp(health, 0, maxHealth);
        CurrentShield = Mathf.Clamp(shield, 0, maxShield);
    }

    public void Die()
    {
        gameObject.SetActive(false);
    }
}
