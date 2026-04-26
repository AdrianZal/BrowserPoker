namespace Poker.Game;

public class BJDeck
{
    private List<Card> _unshuffledDeck = new();
    private Queue<Card> _shuffledDeck = new();

    public BJDeck()
    {
        InitDeck();
        Shuffle();
    }

    public void Shuffle()
    {
        _shuffledDeck.Clear();
        var deck = new List<Card>(_unshuffledDeck);
        var random = new Random();
        var n = 52;
        for (var i = 0; i < 52; i++)
        {
            n--;
            var k = random.Next(n + 1);
            _shuffledDeck.Enqueue(deck[k]);
            deck.Remove(deck[k]);
        }
    }

    private void InitDeck()
    {
        char[] suits = { 'd', 'h', 's', 'c' };
        for (var value = 2; value <= 14; value++)
        {
            for (var i = 0; i < 5; i++)
            {
                foreach (var suit in suits)
                {
                    _unshuffledDeck.Add(new Card(value, suit));
                }
            }
        }
    }

    public Card Draw()
    {
        return _shuffledDeck.Dequeue();
    }

}
