using UnityEngine;
using TMPro;
using System.Collections;

public class TableauLayoutManager : MonoBehaviour
{
    public static TableauLayoutManager Instance;

    [Header("Layout Settings")]
    [SerializeField] private Vector2 tableauStart = new Vector2(-3.2f, 0.5f);
    [SerializeField] private float columnSpacing = 1.1f;
    [SerializeField] private float cardVerticalSpacing = 0.4f;
    [SerializeField] private float faceDownOffset = 0.22f;

    [Header("Card Prefab")]
    [SerializeField] private GameObject cardPrefab;
    public void SetCardPrefab(GameObject prefab) { cardPrefab = prefab; }

    [Header("Foundation Positions")]
    [SerializeField] private Vector2 foundationStart = new Vector2(-3.0f, 2.0f);
    [SerializeField] private float foundationSpacing = 1.8f;

    [Header("Stock/Waste Positions")]
    [SerializeField] private Vector2 stockPosition = new Vector2(-3.8f, 2.0f);
    [SerializeField] private Vector2 wastePosition = new Vector3(-2.3f, 2.0f);
    [SerializeField] private float wasteSpread = 0.3f;

    [Header("Deal Animation")]
    public float dealSpeed = 0.1f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void BuildTableau()
    {
        StartCoroutine(AnimateDeal());
    }

    IEnumerator AnimateDeal()
    {
        // Clear existing cards
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        var piles = BoardManager.Instance.GetTableauPiles();
        
        // Animate each card - deal in order (first card of each pile, then second, etc.)
        int maxRow = 0;
        foreach (var p in piles) maxRow = Mathf.Max(maxRow, p.Count);

        for (int row = 0; row < maxRow; row++)
        {
            for (int col = 0; col < piles.Length; col++)
            {
                if (row >= piles[col].Count) continue;

                CardData card = piles[col][row];
                Vector3 targetPos = new Vector3(
                    tableauStart.x + col * columnSpacing,
                    tableauStart.y - row * (card.isFaceUp ? cardVerticalSpacing : faceDownOffset),
                    0f
                );

                // Start from above the screen
                Vector3 startPos = targetPos + new Vector3(0, 6f, 0);

                GameObject go = Instantiate(cardPrefab, startPos, Quaternion.identity, transform);
                go.name = string.Format("{0}{1}_C{2}R{3}", card.ValueName, card.suit, col, row);
                go.transform.localScale = Vector3.one * 1.4f;

                var visual = go.GetComponent<CardVisualController>();
                if (visual != null) visual.SetupVisuals(card);

                var input = go.GetComponent<CardInputHandler>();
                if (input != null) input.SetTableauInfo(col, row);

                int order = row * 10;
                var allRenderers = go.GetComponentsInChildren<SpriteRenderer>();
                foreach (var r in allRenderers)
                {
                    if (r.name == "GlowAmbient") r.sortingOrder = order - 2;
                    else if (r.name == "GlowOuter") r.sortingOrder = order - 1;
                    else if (r.name == "GlowInner") r.sortingOrder = order;
                    else if (r.name == "Border") r.sortingOrder = order + 2;
                    else r.sortingOrder = order + 1;  // Card face / bg
                }
                var textMeshRenderers = go.GetComponentsInChildren<TextMeshPro>();
                foreach (var tmp in textMeshRenderers)
                    tmp.GetComponent<Renderer>().sortingOrder = order + 3;

                // Animate slide-in
                float animT = 0f;
                while (animT < 1f)
                {
                    animT += Time.deltaTime / dealSpeed;
                    go.transform.position = Vector3.Lerp(startPos, targetPos, Mathf.SmoothStep(0f, 1f, animT));
                    yield return null;
                }
                go.transform.position = targetPos;
            }
            // Small pause between rows
            yield return new WaitForSeconds(dealSpeed * 0.5f);
        }

        Debug.Log("Tableau built visually with animation!");
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
