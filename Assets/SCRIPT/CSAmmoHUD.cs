using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CSAmmoHUD : MonoBehaviour
{
    [Header("HUD Elements")]
    public TextMeshProUGUI currentAmmoText;
    public Image ammoUnderlineBar;
    public TextMeshProUGUI reserveMagsText;
    public Image magazineIconImage;

    [Header("Colors")]
    public Color normalGreen = new Color(0.32f, 1f, 0.29f, 1f); // CS:GO neon HUD green
    public Color lowAmmoRed = new Color(1f, 0.22f, 0.22f, 1f);

    void Start()
    {
        // Procedurally generate a crisp, high-res curved rifle magazine icon
        if (magazineIconImage != null && magazineIconImage.sprite == null)
        {
            magazineIconImage.sprite = CreateCurvedMagSprite();
        }
    }

    public void UpdateHUD(int currentAmmo, int maxAmmo, int reserveMags, bool isReloading)
    {
        bool isLow = !isReloading && currentAmmo <= 7;
        Color activeColor = isLow ? lowAmmoRed : normalGreen;

        if (currentAmmoText != null)
        {
            currentAmmoText.text = isReloading ? "--" : currentAmmo.ToString();
            currentAmmoText.color = activeColor;
        }

        if (ammoUnderlineBar != null)
        {
            ammoUnderlineBar.fillAmount = isReloading ? 0f : (float)currentAmmo / maxAmmo;
            ammoUnderlineBar.color = activeColor;
        }

        if (reserveMagsText != null)
        {
            reserveMagsText.text = reserveMags.ToString();
            reserveMagsText.color = normalGreen;
        }

        if (magazineIconImage != null)
        {
            magazineIconImage.color = normalGreen;
        }
    }

    // Generates a crisp 128x192 curved CS:GO STANAG/AK magazine outline with ribs
    private Sprite CreateCurvedMagSprite()
    {
        int w = 128, h = 192;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        Color clear = new Color(0, 0, 0, 0);
        for (int y = 0; y < h; y++)
        {
            float t = (float)y / h; // 0 = bottom of mag, 1 = top
            // Curve right as it goes down, just like the screenshot
            float centerX = 44f + Mathf.Pow(1f - t, 1.85f) * 34f;
            float halfWidth = 22f;

            for (int x = 0; x < w; x++)
            {
                if (y < 14 || y > h - 14) { tex.SetPixel(x, y, clear); continue; }

                float dx = Mathf.Abs(x - centerX);
                bool isOuterBorder = (dx <= halfWidth && dx >= halfWidth - 5f) || y <= 19 || y >= h - 19;
                bool isInnerRib = (dx < halfWidth - 8f) && (Mathf.Abs(dx - 5f) < 2f) && (y > 26 && y < h - 26);
                bool isFloorplate = (y >= 14 && y <= 22) && (dx <= halfWidth + 3f);

                if (dx <= halfWidth + 3f && (isOuterBorder || isInnerRib || isFloorplate))
                    tex.SetPixel(x, y, Color.white);
                else
                    tex.SetPixel(x, y, clear);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }
}