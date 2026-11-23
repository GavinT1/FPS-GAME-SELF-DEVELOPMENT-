using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float lowHealthThreshold = 25f;

    [Header("CS Health HUD References")]
    public TextMeshProUGUI healthText;
    public Image healthUnderlineBar;
    public Image damageFlashOverlay; // Optional full-screen red flash image

    [Header("Colors")]
    public Color normalGreen = new Color(0.32f, 1f, 0.29f, 1f); // Exact match to your Ammo HUD
    public Color lowHealthRed = new Color(1f, 0.22f, 0.22f, 1f);

    private Vector3 spawnPosition;
    private CharacterController controller;
    private float flashAlpha = 0f;

    void Start()
    {
        currentHealth = maxHealth;
        spawnPosition = transform.position;
        controller = GetComponent<CharacterController>();
        UpdateHealthHUD();
    }

    void Update()
    {
        // Smoothly fade out red screen flash after taking damage
        if (damageFlashOverlay != null && flashAlpha > 0f)
        {
            flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, Time.deltaTime * 1.4f);
            Color c = damageFlashOverlay.color;
            c.a = flashAlpha;
            damageFlashOverlay.color = c;
        }
    }

    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0f) return;

        currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);
        flashAlpha = 0.35f;
        Debug.Log("YOU TOOK DAMAGE! HP: " + Mathf.Max(0, currentHealth));
        UpdateHealthHUD();

        if (currentHealth <= 0f)
        {
            Respawn();
        }
    }

    public void Respawn()
    {
        currentHealth = maxHealth;
        flashAlpha = 0f;

        if (damageFlashOverlay != null)
        {
            Color c = damageFlashOverlay.color;
            c.a = 0f;
            damageFlashOverlay.color = c;
        }

        if (controller != null) controller.enabled = false;
        transform.position = spawnPosition;
        if (controller != null) controller.enabled = true;

        UpdateHealthHUD();
        Debug.Log("YOU DIED! Respawned at 100 HP.");
    }

    private void UpdateHealthHUD()
    {
        int displayHP = Mathf.CeilToInt(currentHealth);
        bool isLow = displayHP <= lowHealthThreshold;
        Color activeColor = isLow ? lowHealthRed : normalGreen;

        if (healthText != null)
        {
            healthText.text = displayHP.ToString();
            healthText.color = activeColor;
        }

        if (healthUnderlineBar != null)
        {
            healthUnderlineBar.fillAmount = currentHealth / maxHealth;
            healthUnderlineBar.color = activeColor;
        }
    }
}