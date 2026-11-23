using UnityEngine;

public class BotHitbox : MonoBehaviour
{
    public TacticalBotAI botAI;
    public float damageMultiplier = 1f; // Set to 4f on HeadHitbox!

    public void OnHit(float baseDamage)
    {
        if (botAI != null)
        {
            botAI.TakeDamage(baseDamage * damageMultiplier, damageMultiplier > 1.5f);
        }
    }
}