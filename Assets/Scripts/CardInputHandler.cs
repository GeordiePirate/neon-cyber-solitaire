using UnityEngine;

public class CardInputHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CardVisualController visualController;
    [SerializeField] private Collider2D cardCollider;

    private Vector3 offset;
    private Vector3 originalPosition;
    private int originalSortingOrder;
    private int tableauIndex = -1;
    private int cardIndexInPile = -1;
    private bool isDragging = false;
    private bool hasMoved = false;
    private const float DRAG_THRESHOLD = 0.15f; // world units before it's a drag

    private void Awake()
    {
        if (visualController == null)
            visualController = GetComponent<CardVisualController>();
        if (cardCollider == null)
            cardCollider = GetComponent<Collider2D>();
    }

    private void OnMouseDown()
    {
        // Block input if game is won or paused
        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameStateManager.State.Playing)
            return;

        CardData data = visualController.GetCardData();
        if (data == null || !data.isFaceUp) return;

        originalPosition = transform.position;
        originalSortingOrder = transform.GetComponent<SpriteRenderer>()?.sortingOrder ?? 0;

        visualController.SetSortingOrder(100);

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        offset = transform.position - new Vector3(mouseWorldPos.x, mouseWorldPos.y, 0);

        isDragging = true;
        hasMoved = false;
    }

    private void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 target = new Vector3(mouseWorldPos.x, mouseWorldPos.y, 0) + offset;

        // Track if the card actually moved from its start position
        if (!hasMoved && Vector3.Distance(target, originalPosition) > DRAG_THRESHOLD)
            hasMoved = true;

        transform.position = target;
    }

    private void OnMouseUp()
    {
        if (!isDragging) return;
        isDragging = false;

        // ── TAP: auto-move to foundation (double-tap or single-tap if valid) ──
        if (!hasMoved)
        {
            CardData myData = visualController.GetCardData();
            if (myData != null)
            {
                int fIdx = BoardManager.Instance.GetFoundationIndexForSuit(myData.suit);
                if (BoardManager.Instance.CanMoveToFoundation(myData, fIdx))
                {
                    // Animated auto-foundation
                    StartCoroutine(AutoMoveToFoundation(fIdx));
                    return;
                }
            }
            // Can't auto-move — just return
            ReturnToOriginalPosition();
            return;
        }

        // ── DRAG: raycast to find drop target ──
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.zero);

        if (hit.collider != null)
        {
            CardInputHandler targetHandler = hit.collider.GetComponent<CardInputHandler>();
            if (targetHandler != null && targetHandler != this)
            {
                CardData targetData = targetHandler.visualController.GetCardData();
                CardData myData = visualController.GetCardData();

                if (targetData != null && myData != null)
                {
                    // Get target tableau index
                    int targetTableauIndex = targetHandler.tableauIndex;
                    if (targetTableauIndex >= 0)
                    {
                        // Try to move
                        if (BoardManager.Instance.MoveToTableau(tableauIndex, cardIndexInPile, targetTableauIndex))
                        {
                            ScoreManager.Instance?.AddPoints(5);
                            visualController.PlayGlitchEffect();
                            // Snap to new position - the layout manager will handle this
                            transform.position = targetHandler.transform.position + new Vector3(0, -0.35f, -0.01f);
                            originalPosition = transform.position;
                            // Re-check cards in new pile for visual update
                            visualController.SetSortingOrder(originalSortingOrder);
                            return;
                        }
                    }
                }
            }
            else
            {
                // Maybe dropped on foundation area
                FoundationDropZone dropZone = hit.collider.GetComponent<FoundationDropZone>();
                if (dropZone != null)
                {
                    CardData myData = visualController.GetCardData();
                    if (myData != null)
                    {
                        if (BoardManager.Instance.MoveToFoundation(myData, tableauIndex))
                        {
                            ScoreManager.Instance?.AddPoints(10);
                            visualController.PlayGlitchEffect();
                            Destroy(gameObject);
                            return;
                        }
                    }
                }
            }
        }

        // Invalid — snap back
        ReturnToOriginalPosition();
    }

    private void ReturnToOriginalPosition()
    {
        transform.position = originalPosition;
        visualController.SetSortingOrder(originalSortingOrder);
    }

    public void SetTableauInfo(int index, int cardIdx)
    {
        tableauIndex = index;
        cardIndexInPile = cardIdx;
    }

    public int GetTableauIndex() => tableauIndex;

    public void SetOriginalPosition(Vector3 pos)
    {
        originalPosition = pos;
        transform.position = pos;
    }

    public void UpdateSortingOrder(int order)
    {
        originalSortingOrder = order;
        visualController.SetSortingOrder(order);
    }

    // ─── Auto-Move to Foundation (tap) ─────────────────────────

    private System.Collections.IEnumerator AutoMoveToFoundation(int foundationIndex)
    {
        CardData data = visualController.GetCardData();
        if (data == null) yield break;

        // Fly to foundation position
        Vector3 start = transform.position;
        Vector3 end = TableauLayoutManager.Instance.GetFoundationPosition(foundationIndex);
        float duration = 0.25f;
        float t = 0f;

        // Play glitch effect during flight
        visualController.PlayGlitchEffect();

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t));
            // Shrink as it approaches
            float s = Mathf.Lerp(0.62f, 0.35f, t);
            transform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        // Execute the move
        BoardManager.Instance.MoveToFoundation(data, tableauIndex);
        ScoreManager.Instance?.AddPoints(10);

        // Particle burst
        visualController.PlayGlitchEffect();

        Destroy(gameObject);
    }
}
