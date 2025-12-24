using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private Image healthFill;
    [SerializeField] private Image shieldFill;

    private PlayerHealth playerHealth;

    public void Bind(PlayerHealth health)
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateUI;
        }

        playerHealth = health;
        playerHealth.OnHealthChanged += UpdateUI;

        UpdateUI(
            playerHealth.Health,
            playerHealth.maxHealth,
            playerHealth.Shield,
            playerHealth.maxShield
        );
    }

    void UpdateUI(int health, int maxHealth, int shield, int maxShield)
    {
        healthFill.fillAmount = (float)health / maxHealth;
        shieldFill.fillAmount = (float)shield / maxShield;
    }

    void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateUI;
        }
    }
}
