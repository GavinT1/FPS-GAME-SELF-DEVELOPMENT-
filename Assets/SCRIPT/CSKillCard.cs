using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class CSKillCard : MonoBehaviour
{
    public static CSKillCard Instance;

    [Header("UI References")]
    public TextMeshProUGUI killCountText;
    public Image gridCardImage;
    public Image medalBadgeImage;
    public RectTransform cardContainer; // Set to CSKillFeedTracker's RectTransform

    [Header("Art Styling")]
    public Color cyberGreen = new Color(0.32f, 1f, 0.29f, 1f);
    public Color medalGold = new Color(1f, 0.84f, 0.35f, 1f);

    private CanvasGroup canvasGroup;
    private int currentKillStreak = 0;
    private float startY;
    private Coroutine activeAnimation;

    void Awake()
    {
        Instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f; // Hide it initially
        
        if (cardContainer == null)
            cardContainer = GetComponent<RectTransform>();
            
        startY = cardContainer.anchoredPosition.y;
    }

    public void TriggerKill()
    {
        currentKillStreak++;
        
        if (killCountText != null)
            killCountText.text = currentKillStreak.ToString();

        // Restart bounce & fade loop
        if (activeAnimation != null) StopCoroutine(activeAnimation);
        activeAnimation = StartCoroutine(AnimateKillCard());
    }

    private IEnumerator AnimateKillCard()
    {
        canvasGroup.alpha = 1f;
        float duration = 0.25f;
        float elapsed = 0f;

        // Snappy slide + elastic bounce up
        Vector2 targetPos = new Vector2(0, startY + 45f);
        Vector2 startPos = new Vector2(0, startY - 80f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Elastic out curve
            t = Mathf.Sin(t * Mathf.PI * 0.5f);
            cardContainer.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        // Play subtle shake/impact
        Vector2 finalPos = targetPos;
        for (int i = 0; i < 4; i++)
        {
            cardContainer.anchoredPosition = finalPos + Random.insideUnitCircle * 4f;
            yield return new WaitForSeconds(0.02f);
        }
        cardContainer.anchoredPosition = finalPos;

        // High visibility hold time
        yield return new WaitForSeconds(2.2f);

        // Slide up further and fade away
        elapsed = 0f;
        float fadeDuration = 0.4f;
        Vector3 fadeStartPos = cardContainer.anchoredPosition;
        Vector3 fadeEndPos = fadeStartPos + new Vector3(0, 50f, 0);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            cardContainer.anchoredPosition = Vector2.Lerp(fadeStartPos, fadeEndPos, t);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }
}