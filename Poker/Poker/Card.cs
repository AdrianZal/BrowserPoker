namespace Poker;

public class Card
{
    public int Value { get; }
    public char Suit { get; }

    public Card(int value, char suit)
    {
        Value = value;
        Suit = suit;
    }
}