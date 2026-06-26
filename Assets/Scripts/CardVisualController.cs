using System.Collections;
using UnityEngine;
using TMPro;

public class CardVisualController : MonoBehaviour
{
    [Header("Sprite Renderers")]
    [SerializeField] private SpriteRenderer borderRenderer;
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private SpriteRenderer glowInner;
    [SerializeField] private SpriteRenderer glowOuter;
    [SerializeField] private SpriteRenderer glowAmbient;

    [Header("Text Elements")]
    [SerializeField] private TextMeshPro rankTL;
    [SerializeField] private TextMeshPro suitTL;
    [SerializeField] private TextMeshPro centerSuit;
    [SerializeField] private TextMeshPro rankBR;
    [SerializeField] private TextMeshPro suitBR;

    [Header("Neon Colors (HDR enabled in Editor)")]
    [ColorUsage(true, true)] public Color redSuitColor = new Color(1f, 0.08f, 0.58f, 1f);   // Neon Pink
    [ColorUsage(true, true)] public Color blackSuitColor = new Color(0f, 0.88f, 1f, 1f);     // Electric Cyan
    [ColorUsage(true, true)] public Color scanGlowColor = new Color(0.2f, 1f, 0.2f, 1f);     // Wireframe Green
    [ColorUsage(true, true)] public Color faceDownColor = new Color(0.3f, 0.3f, 0.4f, 1f);   // Dim circuit

    [Header("Glow Animation")]
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.25f;

    private CardData cardData;
    private SpriteRenderer[] allRenderers;
    private Color baseBorderColor;
    private float time;

    // Holographic scan line effect parameters
    private float holographicAngle = 0f;

    private void Awake()
    {
        CacheRenderers();
    }

    /// <summary>
    /// Runtime hook for Bootstrap to inject renderer references.
    /// </summary>
    public void SetRenderers(SpriteRenderer border, SpriteRenderer bg,
        SpriteRenderer gInner, SpriteRenderer gOuter, SpriteRenderer gAmbient,
        TextMeshPro rankTLRef, TextMeshPro suitTLRef, TextMeshPro centerRef,
        TextMeshPro rankBRRef, TextMeshPro suitBRRef)
    {
        borderRenderer = border;
        backgroundRenderer = bg;
        glowInner = gInner;
        glowOuter = gOuter;
        glowAmbient = gAmbient;
        rankTL = rankTLRef;
        suitTL = suitTLRef;
        centerSuit = centerRef;
        rankBR = rankBRRef;
        suitBR = suitBRRef;
        CacheRenderers();
    }

    private void CacheRenderers()
    {
        allRenderers = GetComponentsInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        AbilityManager.OnNetScanTriggered += HandleNetScan;
    }

    private void OnDisable()
    {
        AbilityManager.OnNetScanTriggered -= HandleNetScan;
    }

    public void SetupVisuals(CardData data)
    {
        cardData = data;
        UpdateVisualState();
    }

    public void UpdateVisualState()
    {
        if (cardData == null) return;

        if (cardData.isFaceUp)
        {
            Color neonColor = cardData.IsRed ? redSuitColor : blackSuitColor;
            borderRenderer.color = neonColor;
            baseBorderColor = neonColor;
            borderRenderer.enabled = true;

            // Try to use pre-rendered card art from Bootstrap atlas
            string suitCode = cardData.suit switch
            {
                Suit.Spades => "S", Suit.Hearts => "H",
                Suit.Diamonds => "D", Suit.Clubs => "C", _ => "S"
            };
            string cardKey = cardData.ValueName + suitCode;
            Sprite cardArt = null;
            if (_Bootstrap.CardSprites != null)
                _Bootstrap.CardSprites.TryGetValue(cardKey, out cardArt);

            if (cardArt != null)
            {
                // Use pre-rendered card art — shows full ornate design
                backgroundRenderer.sprite = cardArt;
                backgroundRenderer.color = Color.white;
                // Hide TMPro overlays — art already has rank/suit baked in
                foreach (var tmp in new[] { rankTL, suitTL, centerSuit, rankBR, suitBR })
                    if (tmp != null) tmp.text = "";
            }
            else
            {
                // Fallback: holographic glass panel with TMPro text
                backgroundRenderer.color = new Color(
                    Mathf.Lerp(1f, neonColor.r, 0.25f),
                    Mathf.Lerp(1f, neonColor.g, 0.25f),
                    Mathf.Lerp(1f, neonColor.b, 0.25f),
                    0.92f);
            }

            // All glow layers — more intense for dramatic bloom
            Color innerColor = new Color(neonColor.r, neonColor.g, neonColor.b, 0.6f);
            Color outerColor = new Color(neonColor.r, neonColor.g, neonColor.b, 0.35f);
            Color ambientColor = new Color(neonColor.r, neonColor.g, neonColor.b, 0.15f);

            SetGlow(glowInner, innerColor, true);
            SetGlow(glowOuter, outerColor, true);
            SetGlow(glowAmbient, ambientColor, true);

            // Card face text
            string rankStr = cardData.ValueName;
            string suitSymbol = cardData.suit switch
            {
                Suit.Hearts => "♥",
                Suit.Diamonds => "♦",
                Suit.Clubs => "♣",
                Suit.Spades => "♠",
                _ => "?"
            };

            if (rankTL != null)
            {
                rankTL.text = rankStr;
                rankTL.color = neonColor;
            }
            if (suitTL != null)
            {
                suitTL.text = suitSymbol;
                suitTL.color = neonColor;
            }
            if (centerSuit != null)
            {
                centerSuit.text = suitSymbol;
                // Center suit — semi-transparent holographic symbol, clearly visible
                centerSuit.color = new Color(neonColor.r, neonColor.g, neonColor.b, 0.45f);
                // Keep large font size that bootstrap set (14+) — don't override small
            }
            if (rankBR != null)
            {
                rankBR.text = rankStr;
                rankBR.color = neonColor;
            }
            if (suitBR != null)
            {
                suitBR.text = suitSymbol;
                suitBR.color = neonColor;
            }
        }
        else
        {
            borderRenderer.color = faceDownColor;
            borderRenderer.enabled = true;
            // Face-down card — use card back sprite or fallback circuit pattern
            if (_Bootstrap.CardBackSprite != null)
            {
                backgroundRenderer.sprite = _Bootstrap.CardBackSprite;
                backgroundRenderer.color = Color.white;
            }
            else
            {
                backgroundRenderer.color = Color.white;
            }

            SetGlow(glowInner, Color.clear, false);
            SetGlow(glowOuter, Color.clear, false);
            SetGlow(glowAmbient, Color.clear, false);

            // Hide all card face text
            foreach (var tmp in new[] { rankTL, suitTL, centerSuit, rankBR, suitBR })
            {
                if (tmp != null) tmp.text = "";
            }
        }
    }

    public void PlayGlitchEffect()
    {
        StartCoroutine(GlitchRoutine());
    }

    private IEnumerator GlitchRoutine()
    {
        if (borderRenderer == null) yield break;

        float duration = 0.3f;
        float elapsed = 0f;
        Color orig = borderRenderer.color;

        while (elapsed < duration)
        {
            // Strobe the border
            borderRenderer.enabled = Random.value > 0.5f;
            if (borderRenderer.enabled)
            {
                borderRenderer.color = new Color(1f, 1f, 1f, Random.Range(0.5f, 1f));
            }
            elapsed += Time.deltaTime * 0.05f;
            yield return new WaitForSeconds(Random.Range(0.02f, 0.08f));
        }

        borderRenderer.enabled = true;
        borderRenderer.color = orig;
        UpdateVisualState();
    }

    void Update()
    {
        time += Time.deltaTime;
        holographicAngle += Time.deltaTime * 15f;

        if (cardData != null && cardData.isFaceUp && borderRenderer.enabled)
        {
            float pulse = 1f + Mathf.Sin(time * pulseSpeed) * pulseAmount;
            borderRenderer.color = baseBorderColor * pulse;

            // Pulse all glow layers with phase offsets for organic feel
            if (glowInner != null && glowInner.enabled)
            {
                float gp = 0.5f + Mathf.Sin(time * pulseSpeed * 0.7f) * 0.2f;
                glowInner.color = new Color(baseBorderColor.r, baseBorderColor.g, baseBorderColor.b, gp);
                glowInner.transform.localScale = Vector3.one * (1.15f + Mathf.Sin(time * pulseSpeed * 0.5f) * 0.12f);
            }
            if (glowOuter != null && glowOuter.enabled)
            {
                float gp = 0.25f + Mathf.Sin(time * pulseSpeed * 0.5f + 1f) * 0.15f;
                glowOuter.color = new Color(baseBorderColor.r, baseBorderColor.g, baseBorderColor.b, gp);
                glowOuter.transform.localScale = Vector3.one * (2.0f + Mathf.Sin(time * pulseSpeed * 0.4f + 2f) * 0.4f);
            }
            if (glowAmbient != null && glowAmbient.enabled)
            {
                float gp = 0.1f + Mathf.Sin(time * pulseSpeed * 0.3f + 3f) * 0.06f;
                glowAmbient.color = new Color(baseBorderColor.r, baseBorderColor.g, baseBorderColor.b, gp);
                glowAmbient.transform.localScale = Vector3.one * (3.5f + Mathf.Sin(time * pulseSpeed * 0.25f + 1.5f) * 0.6f);
            }

            // Pulse all card face labels
            foreach (var tmp in new[] { rankTL, suitTL, rankBR, suitBR })
            {
                if (tmp != null && !string.IsNullOrEmpty(tmp.text))
                    tmp.color = baseBorderColor * pulse;
            }

            // Subtle center-suit breathing
            if (centerSuit != null && !string.IsNullOrEmpty(centerSuit.text))
            {
                float centerPulse = 0.35f + Mathf.Sin(time * 1.2f) * 0.12f;
                Color c = centerSuit.color;
                c.a = centerPulse;
                centerSuit.color = c;
            }
        }
    }

    private void SetGlow(SpriteRenderer glow, Color color, bool enabled)
    {
        if (glow != null)
        {
            glow.enabled = enabled;
            glow.color = color;
        }
    }

    private void HandleNetScan(float duration)
    {
        if (cardData != null && !cardData.isFaceUp)
        {
            StartCoroutine(FlashScanRoutine(duration));
        }
    }

    private IEnumerator FlashScanRoutine(float duration)
    {
        borderRenderer.color = scanGlowColor;
        // Reveal card on scan
        if (centerSuit != null)
        {
            string suitSymbol = cardData.suit switch
            {
                Suit.Hearts => "♥",
                Suit.Diamonds => "♦",
                Suit.Clubs => "♣",
                Suit.Spades => "♠",
                _ => "?"
            };
            centerSuit.text = suitSymbol;
            centerSuit.color = scanGlowColor;
            centerSuit.fontSize = 14f;
        }
        if (rankTL != null)
        {
            rankTL.text = cardData.ValueName;
            rankTL.color = scanGlowColor;
        }

        // Enable glow layers during scan
        SetGlow(glowInner, scanGlowColor * 0.6f, true);
        SetGlow(glowOuter, scanGlowColor * 0.3f, true);
        SetGlow(glowAmbient, scanGlowColor * 0.15f, true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float pulse = 1f + Mathf.Sin(elapsed * 12f) * 0.3f;
            borderRenderer.color = scanGlowColor * pulse;
            elapsed += Time.deltaTime;
            yield return null;
        }

        UpdateVisualState();
    }

    public void HighlightValidTarget(bool valid)
    {
        if (valid)
            borderRenderer.color = scanGlowColor;
        else
            UpdateVisualState();
    }

    public void SetSortingOrder(int order)
    {
        foreach (var renderer in allRenderers)
            renderer.sortingOrder = order;
    }

    public CardData GetCardData() => cardData;
}
