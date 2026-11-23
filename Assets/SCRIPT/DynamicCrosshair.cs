using UnityEngine;

public class DynamicCrosshair : MonoBehaviour
{
    [Header("Crosshair Ticks")]
    public RectTransform topTick;
    public RectTransform bottomTick;
    public RectTransform leftTick;
    public RectTransform rightTick;

    [Header("Gap Settings")]
    public float restingGap = 6f;
    public float maxGap = 35f;
    public float recoverSpeed = 12f;

    [HideInInspector] public float currentSpread = 0f;

    void Update()
    {
        // Smoothly shrink the crosshair back to its tight resting gap
        currentSpread = Mathf.Lerp(currentSpread, 0f, Time.deltaTime * recoverSpeed);

        float totalGap = Mathf.Clamp(restingGap + currentSpread, restingGap, maxGap);

        if (topTick != null) topTick.anchoredPosition = new Vector2(0, totalGap);
        if (bottomTick != null) bottomTick.anchoredPosition = new Vector2(0, -totalGap);
        if (leftTick != null) leftTick.anchoredPosition = new Vector2(-totalGap, 0);
        if (rightTick != null) rightTick.anchoredPosition = new Vector2(totalGap, 0);
    }

    public void AddSpread(float amount)
    {
        currentSpread = Mathf.Clamp(currentSpread + amount, 0f, maxGap - restingGap);
    }

    // Converts the visual UI gap into an accurate bullet spread angle
    public float GetSpreadAngle()
    {
        return currentSpread * 0.002f;
    }
}