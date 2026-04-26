namespace Poker.Game;
public class BJTable
{
    private BJDeck Deck { get; set; }
    public Player Player { get; set; }
    public BJTable(Player player)
    {
        Deck = new BJDeck();
        Player = player;
    }
}
