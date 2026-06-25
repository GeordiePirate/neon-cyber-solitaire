using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance;

    [Header("Draw Mode")]
    [SerializeField] private bool drawThree = false; // false = Draw 1, true = Draw 3

    private List<CardData> masterDeck = new List<CardData>();
    private List<CardData> stockPile = new List<CardData>();
    private List<CardData> wastePile = new List<CardData>();
    private List<CardData>[] tableauPiles = new List<CardData>[7];
    private List<CardData>[] foundationPiles = new List<CardData>[4];

    public System.Action OnBoardDealt;
    public System.Action<CardData> OnCardMovedToFoundation;
    public System.Action<int> OnCardRevealed; // tableau pile index

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        InitializeDeck();
        ShuffleDeck();
        DealGame();
    }

    void InitializeDeck()
    {
        masterDeck.Clear();
        foreach (Suit suit in System.Enum.GetValues(typeof(Suit)))
        {
            for (int val = 1; val <= 13; val++)
            {
                masterDeck.Add(new CardData(suit, val));
            }
        }
    }

    void ShuffleDeck()
    {
        // Fisher-Yates Shuffle
        for (int i = masterDeck.Count - 1; i > 0; i--)
        {
            int rnd = Random.Range(0, i + 1);
            CardData temp = masterDeck[i];
            masterDeck[i] = masterDeck[rnd];
            masterDeck[rnd] = temp;
        }
        stockPile = new List<CardData>(masterDeck);
    }

    void DealGame()
    {
        for (int i = 0; i < 7; i++)
        {
            tableauPiles[i] = new List<CardData>();
            for (int j = 0; j <= i; j++)
            {
                CardData card = stockPile[0];
                stockPile.RemoveAt(0);
                if (j == i) card.isFaceUp = true;
                tableauPiles[i].Add(card);
            }
        }

        for (int i = 0; i < 4; i++)
        {
            foundationPiles[i] = new List<CardData>();
        }

        OnBoardDealt?.Invoke();
        Debug.Log("Board dealt successfully.");
    }

    // ─── Stock / Waste ────────────────────────────────────────

    public void DrawFromStock()
    {
        if (stockPile.Count == 0)
        {
            // Reshuffle waste back into stock
            stockPile = new List<CardData>(wastePile);
            wastePile.Clear();
            foreach (var card in stockPile)
                card.isFaceUp = false;

            Debug.Log("Stock reshuffled from waste.");
            return;
        }

        int drawCount = drawThree ? 3 : 1;
        for (int i = 0; i < drawCount && stockPile.Count > 0; i++)
        {
            CardData card = stockPile[0];
            stockPile.RemoveAt(0);
            card.isFaceUp = true;
            wastePile.Add(card);
        }
    }

    // ─── Move Validation ─────────────────────────────────────

    public bool CanMoveToTableau(CardData card, int targetTableauIndex)
    {
        var pile = tableauPiles[targetTableauIndex];
        if (pile.Count == 0) return card.value == 13; // Only Kings on empty piles
        CardData top = pile[pile.Count - 1];
        return top.isFaceUp && top.value == card.value + 1 && top.IsRed != card.IsRed;
    }

    public bool CanMoveToFoundation(CardData card, int foundationIndex)
    {
        var pile = foundationPiles[foundationIndex];
        if (pile.Count == 0) return card.value == 1; // Ace first
        CardData top = pile[pile.Count - 1];
        return top.suit == card.suit && top.value == card.value - 1;
    }

    public int GetFoundationIndexForSuit(Suit suit) => suit switch
    {
        Suit.Hearts => 0,
        Suit.Diamonds => 1,
        Suit.Clubs => 2,
        Suit.Spades => 3,
        _ => -1
    };

    // ─── Moves ────────────────────────────────────────────────

    public bool MoveToFoundation(CardData card, int tableauIndex)
    {
        int fIdx = GetFoundationIndexForSuit(card.suit);
        if (!CanMoveToFoundation(card, fIdx)) return false;

        tableauPiles[tableauIndex].Remove(card);
        foundationPiles[fIdx].Add(card);
        OnCardMovedToFoundation?.Invoke(card);
        CheckReveal(tableauIndex);
        return true;
    }

    public bool MoveToTableau(int sourceTableauIndex, int cardIndex, int targetTableauIndex)
    {
        var source = tableauPiles[sourceTableauIndex];
        if (cardIndex < 0 || cardIndex >= source.Count) return false;

        CardData movingCard = source[cardIndex];
        if (!movingCard.isFaceUp) return false;
        if (!CanMoveToTableau(movingCard, targetTableauIndex)) return false;

        // Move the card and everything stacked on it
        int count = source.Count - cardIndex;
        var moving = source.GetRange(cardIndex, count);
        source.RemoveRange(cardIndex, count);
        tableauPiles[targetTableauIndex].AddRange(moving);

        CheckReveal(sourceTableauIndex);
        return true;
    }

    public bool MoveFromWasteToTableau(int targetTableauIndex)
    {
        if (wastePile.Count == 0) return false;
        CardData card = wastePile[wastePile.Count - 1];
        if (!CanMoveToTableau(card, targetTableauIndex)) return false;

        wastePile.RemoveAt(wastePile.Count - 1);
        tableauPiles[targetTableauIndex].Add(card);
        return true;
    }

    public bool MoveFromWasteToFoundation()
    {
        if (wastePile.Count == 0) return false;
        CardData card = wastePile[wastePile.Count - 1];
        int fIdx = GetFoundationIndexForSuit(card.suit);
        if (!CanMoveToFoundation(card, fIdx)) return false;

        wastePile.RemoveAt(wastePile.Count - 1);
        foundationPiles[fIdx].Add(card);
        OnCardMovedToFoundation?.Invoke(card);
        return true;
    }

    private void CheckReveal(int tableauIndex)
    {
        var pile = tableauPiles[tableauIndex];
        if (pile.Count > 0 && !pile[pile.Count - 1].isFaceUp)
        {
            pile[pile.Count - 1].isFaceUp = true;
            OnCardRevealed?.Invoke(tableauIndex);
        }
    }

    // ─── Accessors ────────────────────────────────────────────

    public List<CardData> GetTableauPile(int index) => tableauPiles[index];
    public List<CardData>[] GetTableauPiles() => tableauPiles;
    public List<CardData> GetFoundationPile(int index) => foundationPiles[index];
    public List<CardData> GetWastePile() => wastePile;
    public List<CardData> GetStockPile() => stockPile;
    public CardData GetTopOfWaste() => wastePile.Count > 0 ? wastePile[wastePile.Count - 1] : null;
    public bool IsDrawThree => drawThree;

    public bool CheckWin()
    {
        foreach (var pile in foundationPiles)
            if (pile.Count != 13) return false;
        Debug.Log("YOU WIN! All foundations complete.");
        return true;
    }
}
