using UnityEngine;
using UnityEngine.UI;

public class Health : MonoBehaviour
{

    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    public bool isInvincible = false;
    public Image healthBar;
    // Start is called before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        
        if (!isInvincible)
        {
            currentHealth -= damage;
            healthBar.fillAmount = Mathf.Clamp(((float)currentHealth / (float)maxHealth), 0, 1);
        }
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
    }

    private void Die()
    {
        Debug.Log(gameObject.name + " died");
        Destroy(gameObject);
    }
}
