using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int maxShield = 100;

    private int health;
    private int shield;

    public void SetHealth(int newHealth, int newShield)
    {
        health = newHealth;
        shield = newShield;

        // update UI later
    }

    public void Die()
    {
        gameObject.SetActive(false);
    }
}

