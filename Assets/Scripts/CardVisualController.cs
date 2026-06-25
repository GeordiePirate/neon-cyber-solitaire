using System.Collections;
using UnityEngine;

public class CardVisualController : MonoBehaviour
{
    [Header("Sprite Renderers")]
    [SerializeField] private SpriteRenderer borderRenderer;
    [SerializeField] private SpriteRenderer faceSymbolsRenderer;
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private ParticleSystem glitchParticles;

    [Header("Neon Colors (HDR enabled in Editor)")]
    [ColorUsage(true, true)] public Color redSuitColor = new Color(1f, 0.08f, 0.58f, 1f);   // Neon Pink
    [ColorUsage(true, true)] public Color blackSuitColor = new Color(0f, 0.88f, 1f, 1f);     // Electric Cyan
    [ColorUsage(true, true)] public Color scanGlowColor = new Color(0.2f, 1f, 0.2f, 1f);     // Wireframe Green
    [ColorUsage(true, true)] public Color faceDownColor = new Color(0.3f, 0.3f, 0.4f, 1f);   // Dim circuit

    private CardData cardData;
    private SpriteRenderer[] allRenderers;

    private void Awake()
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
            faceSymbolsRenderer.color = neonColor;
            borderRenderer.enabled = true;
            faceSymbolsRenderer.enabled = true;
            backgroundRenderer.color = new Color(0.05f, 0.05f, 0.1f, 1f); // Near-black matte
        }
        else
        {
            borderRenderer.color = faceDownColor;
            faceSymbolsRenderer.enabled = false;
            borderRenderer.enabled = true;
            backgroundRenderer.color = new Color(0.08f, 0.08f, 0.12f, 1f);
        }
    }

    public void PlayGlitchEffect()
    {
        if (glitchParticles != null)
            glitchParticles.Play();
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
        faceSymbolsRenderer.enabled = true;
        faceSymbolsRenderer.color = scanGlowColor;

        // Briefly pulse brighter
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
