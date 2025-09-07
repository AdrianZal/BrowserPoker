namespace Poker;

public class Player
{
    public string name;
    public List<Card> cards;
    public int tableBalance;

    public Player(string name, int buyIn)
    {
        this.name = name;
        this.cards = new List<Card>(2);
        this.tableBalance = buyIn;
    }

    public void SeeCards()
    {
        Console.WriteLine(name);
        Console.WriteLine(cards[0].Value+" "+cards[0].Suit+"\n"+cards[1].Value+" "+cards[1].Suit);
    }
    
}