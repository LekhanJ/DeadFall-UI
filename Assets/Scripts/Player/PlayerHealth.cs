using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int maxShield = 100;

    public int Health { get; private set; }
    public int Shield { get; private set; }

    public bool IsDead => Health <= 0;

    public event Action<int, int, int, int> OnHealthChanged;
    // (health, maxHealth, shield, maxShield)

    void Awake()
    {
        Health = maxHealth;
        Shield = maxShield;
    }

    public void SetHealth(int health, int shield)
    {
        Health = Mathf.Clamp(health, 0, maxHealth);
        Shield = Mathf.Clamp(shield, 0, maxShield);

        OnHealthChanged?.Invoke(
            Health,
            maxHealth,
            Shield,
            maxShield
        );
    }

    public void Die()
    {
        Debug.Log($"{name} died!");
        gameObject.SetActive(false);
    }
}
