using System;

public enum Suit
{
    Hearts,
    Diamonds,
    Clubs,
    Spades
}

[System.Serializable]
public class CardData
{
    public Suit suit;
    public int value; // 1 = Ace, 11 = Jack, 12 = Queen, 13 = King
    public bool isFaceUp;

    public bool IsRed => suit == Suit.Hearts || suit == Suit.Diamonds;

    public string ValueName => value switch
    {
        1 => "A",
        11 => "J",
        12 => "Q",
        13 => "K",
        _ => value.ToString()
    };

    public CardData(Suit suit, int value, bool isFaceUp = false)
    {
        this.suit = suit;
        this.value = value;
        this.isFaceUp = isFaceUp;
    }
}
