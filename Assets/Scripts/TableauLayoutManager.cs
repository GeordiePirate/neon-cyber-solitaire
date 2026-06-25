using UnityEngine;

public class TableauLayoutManager : MonoBehaviour
{
    public static TableauLayoutManager Instance;

    [Header("Layout Settings")]
    [SerializeField] private Vector2 tableauStart = new Vector2(-4.5f, 2f);
    [SerializeField] private float columnSpacing = 1.5f;
    [SerializeField] private float cardVerticalSpacing = 0.35f;
    [SerializeField] private float faceDownOffset = 0.2f;

    [Header("Card Prefab")]
    [SerializeField] private GameObject cardPrefab;

    [Header("Foundation Positions")]
    [SerializeField] private Vector2 foundationStart = new Vector2(1.5f, 4.5f);
    [SerializeField] private float foundationSpacing = 1.5f;

    [Header("Stock/Waste Positions")]
    [SerializeField] private Vector2 stockPosition = new Vector2(-4.5f, 4.5f);
    [SerializeField] private Vector2 wastePosition = new Vector2(-3f, 4.5f);
    [SerializeField] private float wasteSpread = 0.3f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        BoardManager.Instance.OnBoardDealt += BuildTableau;
    }

    public void BuildTableau()
    {
        // Clear existing cards
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        // Build each tableau pile
        var piles = BoardManager.Instance.GetTableauPiles();
        for (int col = 0; col < piles.Length; col++)
        {
            for (int row = 0; row < piles[col].Count; row++)
            {
                CardData card = piles[col][row];
                Vector3 pos = new Vector3(
                    tableauStart.x + col * columnSpacing,
                    tableauStart.y - row * (card.isFaceUp ? cardVerticalSpacing : faceDownOffset),
                    0f
                );

                GameObject go = Instantiate(cardPrefab, pos, Quaternion.identity, transform);
                go.name = $"{card.ValueName}{card.suit}_C{col}R{row}";

                // Wire up visual
                var visual = go.GetComponent<CardVisualController>();
                if (visual != null) visual.SetupVisuals(card);

                // Wire up input handler
                var input = go.GetComponent<CardInputHandler>();
                if (input != null) input.SetTableauInfo(col, row);

                // Sorting order = pile depth
                int order = row * 10;
                var renderers = go.GetComponentsInChildren<SpriteRenderer>();
                foreach (var r in renderers)
                    r.sortingOrder = order;
            }
        }

        // Build foundation drop zones
        for (int i = 0; i < 4; i++)
        {
            Vector3 pos = new Vector3(
                foundationStart.x + i * foundationSpacing,
                foundationStart.y,
                0f
            );
            // Foundation zone visuals would go here
        }

        Debug.Log("Tableau built visually.");
    }

    public Vector3 GetCardPosition(int tableauIndex, int cardIndex)
    {
        var pile = BoardManager.Instance.GetTableauPile(tableauIndex);
        if (cardIndex >= pile.Count) return Vector3.zero;

        CardData card = pile[cardIndex];
        return new Vector3(
            tableauStart.x + tableauIndex * columnSpacing,
            tableauStart.y - cardIndex * (card.isFaceUp ? cardVerticalSpacing : faceDownOffset),
            -cardIndex * 0.01f
        );
    }

    public Vector3 GetStockPosition() => stockPosition;
    public Vector3 GetWastePosition(int index) => new Vector3(
        wastePosition.x + index * wasteSpread,
        wastePosition.y,
        -index * 0.01f
    );
}
