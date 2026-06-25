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

    private void Awake()
    {
        if (visualController == null)
            visualController = GetComponent<CardVisualController>();
        if (cardCollider == null)
            cardCollider = GetComponent<Collider2D>();
    }

    private void OnMouseDown()
    {
        CardData data = visualController.GetCardData();
        if (data == null || !data.isFaceUp) return;

        originalPosition = transform.position;
        originalSortingOrder = transform.GetComponent<SpriteRenderer>()?.sortingOrder ?? 0;

        visualController.SetSortingOrder(100);

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        offset = transform.position - new Vector3(mouseWorldPos.x, mouseWorldPos.y, 0);

        isDragging = true;
    }

    private void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        transform.position = new Vector3(mouseWorldPos.x, mouseWorldPos.y, 0) + offset;
    }

    private void OnMouseUp()
    {
        if (!isDragging) return;
        isDragging = false;

        // Raycast to see what we dropped on
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
}
