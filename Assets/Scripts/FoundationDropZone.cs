using UnityEngine;

public class FoundationDropZone : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int foundationIndex = 0; // 0=Hearts, 1=Diamonds, 2=Clubs, 3=Spades
    [SerializeField] private Suit targetSuit;

    private SpriteRenderer zoneRenderer;

    [Header("Neon Colors")]
    [ColorUsage(true, true)] public Color activeGlow = new Color(0.2f, 1f, 0.2f, 0.3f);
    [ColorUsage(true, true)] public Color inactiveColor = new Color(0.1f, 0.1f, 0.15f, 0.1f);

    private void Awake()
    {
        zoneRenderer = GetComponent<SpriteRenderer>();
        if (zoneRenderer != null)
            zoneRenderer.color = inactiveColor;
    }

    /// <summary>Runtime init for Bootstrap.</summary>
    public void Init(int index)
    {
        foundationIndex = index;
        targetSuit = index switch
        {
            0 => Suit.Hearts,
            1 => Suit.Diamonds,
            2 => Suit.Clubs,
            3 => Suit.Spades,
            _ => Suit.Hearts
        };
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        CardInputHandler handler = other.GetComponent<CardInputHandler>();
        if (handler != null)
        {
            CardData data = handler?.GetComponent<CardVisualController>()?.GetCardData();
            if (data != null && BoardManager.Instance.CanMoveToFoundation(data, foundationIndex))
            {
                if (zoneRenderer != null)
                    zoneRenderer.color = activeGlow;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (zoneRenderer != null)
            zoneRenderer.color = inactiveColor;
    }

    public int GetFoundationIndex() => foundationIndex;
    public Suit GetTargetSuit() => targetSuit;
}
