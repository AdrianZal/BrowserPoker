namespace Poker;

public class Table
{
    private enum PlayerRole
    {
        None,
        Dealer,
        SmallBlind,
        BigBlind
    }

    private enum PlayerStatuses
    {
        Folded,
        ToCall,
        ToCheck,
        Checked,
        AllIn
    }

    public List<Player> players = new(7);
    private Deck deck;
    public List<Card> cards = new(5);
    public int buyIn;
    public int smallBlind;
    public int dealerIndex;
    public int smallBlindIndex;
    public int bigBlindIndex;
    public int minRaise;
    public int toCall;
    Dictionary<Player, int> roundBets = new();
    Dictionary<Player, int> handBets = new();
    Dictionary<Player, PlayerRole> playerRoles = new();
    Dictionary<Player, PlayerStatuses> playerStatuses = new();

    public Table(int buyIn)
    {
        deck = new Deck();
        this.buyIn = buyIn;
        smallBlind = buyIn / 100 / 2;
        minRaise = smallBlind * 2;
    }

    public void PlayRound()
    {
        Deal();
        if (players.Count < 2)
            return;
        ShowTableCards();
        var x =BettingRound(true);
        
        Flop();
        ShowTableCards();
        if (x)
        {
            x = BettingRound(false);
        }
        
        DrawTableCard();
        ShowTableCards();
        if (x)
        {
            x = BettingRound(false);
        }
        
        DrawTableCard();
        ShowTableCards();
        if (x)
        {
            BettingRound(false);
        }

        var winners = ResolveRound();
    }

    private Dictionary<Player, int> ResolveRound()
    {
        Dictionary<Player, int> winningsByPlayer = new();
        Dictionary<Player, int> playerScores = players.Where(p => playerStatuses[p] != PlayerStatuses.Folded).ToDictionary(p => p, CheckScore);
        
        var levels = handBets.Values.Where(v => v > 0).Distinct().OrderBy(v => v).ToList();
        int prev = 0;

        foreach (var level in levels)
        {
            int contribution = level - prev;
            
            var contributors = players.Where(p => handBets[p] >= level);
            int potAmount = contributors.Count() * contribution;

            var eligible = players
                .Where(p => handBets[p] >= level && playerStatuses[p] != PlayerStatuses.Folded)
                .ToList();

            if (eligible.Count == 0)
                continue;

            int bestScore = eligible.Max(p => playerScores[p]);
            var winners = eligible.Where(p => playerScores[p] == bestScore).ToList();

            int share = potAmount / winners.Count;
            int remainder = potAmount % winners.Count;

            foreach (var w in winners)
            {
                w.tableBalance += share;
                if (winningsByPlayer.ContainsKey(w))
                {
                    winningsByPlayer[w] += share;
                }
                else
                {
                    winningsByPlayer.Add(w, share);
                }
            }

            foreach (var w in winners.Take(remainder))
            {
                w.tableBalance += 1;
                winningsByPlayer[w] += 1;
            }
            
            prev = level;
        }

        return winningsByPlayer;
    }
    

    private bool BettingRound(bool preflop)
    {
        BettingRoundReset(preflop);
        var i = smallBlindIndex;
        if (!preflop && players.Count == 2)
        {
            i = bigBlindIndex;
        }
        while (true)
        {
            var player = players[i];
            if (playerStatuses.Count(s => s.Value == PlayerStatuses.Folded) == players.Count - 1)
            {
                return false;
            }
            if (playerStatuses[player] == PlayerStatuses.Folded || playerStatuses[player] == PlayerStatuses.AllIn)
            {
                i = (i + 1) % players.Count;
                continue;
            }
            if (playerStatuses[player] == PlayerStatuses.Checked) 
                break;
            Console.WriteLine(player.name + "(" + playerRoles[player] + ")" + ": ");
            var decision = Console.ReadLine();

            switch (decision)
            {
                case "check":
                    playerStatuses[player] = PlayerStatuses.Checked;
                    break;
                case "fold":
                    playerStatuses[player] = PlayerStatuses.Folded;
                    break;
                case "call":
                    int callAmount = toCall - roundBets[player];
                    if (player.tableBalance <=  callAmount)
                    {
                        roundBets[player] += player.tableBalance;
                        handBets[player] += player.tableBalance;
                        player.tableBalance = 0;
                        playerStatuses[player] = PlayerStatuses.AllIn;
                    }
                    else
                    {
                        roundBets[player] += callAmount;
                        handBets[player] += callAmount;
                        player.tableBalance -= callAmount;
                        playerStatuses[player] = PlayerStatuses.Checked;
                    }
                    break;
                case "raise":
                    Console.WriteLine("Amount: ");
                    var input = Convert.ToInt32(Console.ReadLine());

                    if (input > player.tableBalance)
                    {
                        break;
                    }

                    var amount = input;

                    if (amount < minRaise && amount == player.tableBalance)
                    {
                        roundBets[player] += amount;
                        handBets[player] += amount;
                        player.tableBalance -= amount;
                        toCall = roundBets.Values.Max();
                        playerStatuses[player] = PlayerStatuses.AllIn;
                        break;
                    }

                    minRaise = amount;
                    roundBets[player] += amount;
                    handBets[player] += amount;
                    player.tableBalance -= amount;
                    toCall = roundBets.Values.Max();

                    if (player.tableBalance == 0)
                        playerStatuses[player] = PlayerStatuses.AllIn;
                    else
                        playerStatuses[player] = PlayerStatuses.Checked;
                    foreach (var p in players)
                    {
                        if (p != player && playerStatuses[p] != PlayerStatuses.Folded &&
                            playerStatuses[p] != PlayerStatuses.AllIn)
                        {
                            playerStatuses[p] = PlayerStatuses.ToCall;
                        }
                    }
                    break;
            }
            i = (i + 1) % players.Count;
        }
        return true;
    }

    public void Reindex()
    {
        dealerIndex = NextIndex(dealerIndex);
        if (players.Count == 2)
        {
            smallBlindIndex = dealerIndex;
            bigBlindIndex = NextIndex(dealerIndex);
        }
        else if (players.Count >= 3)
        {
            smallBlindIndex = NextIndex(dealerIndex);
            bigBlindIndex = NextIndex(smallBlindIndex);
        }
        
        playerRoles.Clear();
        for (var i = 0; i < players.Count; i++)
        {
            var player = players[i];
            playerRoles[player] =
                i == dealerIndex ? PlayerRole.Dealer :
                i == smallBlindIndex ? PlayerRole.SmallBlind :
                i == bigBlindIndex ? PlayerRole.BigBlind :
                PlayerRole.None;
        }
    }

    private int NextIndex(int currentIndex)
    {
        return (currentIndex + 1) % players.Count;
    }

    public void BettingRoundReset(bool preflop)
    {
        if (preflop)
        {
            toCall = smallBlind * 2;
            handBets.Clear();
            Reindex();
        }
        else
        {
            toCall =0 ;
        }
        minRaise = smallBlind * 2;
        foreach (var player in players)
        {
            roundBets[player] = 0;
            if (preflop)
            {
                switch (playerRoles[player])
                {
                    case PlayerRole.SmallBlind:
                        roundBets[player] = smallBlind;
                        handBets[player] = smallBlind;
                        player.tableBalance -= smallBlind;
                        playerStatuses[player] = PlayerStatuses.ToCall;
                        break;
                    case PlayerRole.BigBlind:
                        roundBets[player] = smallBlind * 2;
                        handBets[player] = smallBlind * 2;
                        player.tableBalance -= smallBlind * 2;
                        playerStatuses[player] = PlayerStatuses.ToCheck;
                        break;
                    default:
                        handBets[player] = 0;
                        playerStatuses[player] = PlayerStatuses.ToCall;
                        break;
                }
            }
            else
            {
                if (playerStatuses[player] != PlayerStatuses.Folded && playerStatuses[player] != PlayerStatuses.AllIn)
                    playerStatuses[player] = PlayerStatuses.ToCall;
            }
        }
    }

    public void ShowTableCards()
    {
        Console.Clear();
        Console.WriteLine("Pot: " + handBets.Values.Sum() + "$");
        foreach (var player in players)
        {
            Console.WriteLine("Balance: " + player.tableBalance);
            player.SeeCards();
            var score = CheckScore(player);
            Console.WriteLine(WhatHand(score) + "\n" + score + "\n");
        }

        foreach (var card in cards)
        {
            Console.WriteLine(card.Value + " " + card.Suit);
        }
    }


    public void AddPlayer(Player player)
    {
        if (players.Count > 6)
            return;
        players.Add(player);
        roundBets.Add(player, 0);
    }
    
    public void RemovePlayer(Player player)
    {
        players.Remove(player);
    }

    public void Deal()
    {
        cards.Clear();
        deck.Shuffle();
        foreach (var player in players)
        {
            player.cards.Clear();
        }

        for (var i = 0; i < 2; i++)
        {
            foreach (var p in players)
            {
                p.cards.Add(deck.Draw());
            }
        }
    }

    public void Flop()
    {
        for (var i = 0; i < 3; i++)
            DrawTableCard();
    }

    public void DrawTableCard()
    {
        cards.Add(deck.Draw());
    }

    public string WhatHand(int x)
    {
        if (x >= 90000000)
            return "Royal flush";
        if (x >= 8000000)
            return "Straight flush";
        if (x >= 7000000)
            return "Four of a kind";
        if (x >= 6000000)
            return "Full house";
        if (x >= 5000000)
            return "Flush";
        if (x >= 4000000)
            return "Straight";
        if (x >= 3000000)
            return "Three of a kind";
        if (x >= 2000000)
            return "Two pair";
        if (x >= 1000000)
            return "Pair";
        return "High card";
    }

    public int CheckScore(Player player)
    {
        var hand = this.cards.Concat(player.cards).ToList();

        var x = IsRoyalFlush(hand);
        if (x != 0)
        {
            return x;
        }

        x = IsStraightFlush(hand);
        if (x != 0)
        {
            return x;
        }

        x = IsFourOfAKind(hand);
        if (x != 0)
        {
            return x;
        }

        x = IsFullHouse(hand);
        if (x != 0)
        {
            return x;
        }

        x = IsFlush(hand);
        if (x != 0)
        {
            return x;
        }

        x = IsStraight(hand);
        if (x != 0)
        {
            return x;
        }

        x = IsThreeOfAKind(hand);
        if (x != 0)
        {
            return x;
        }

        x = IsTwoPair(hand);
        if (x != 0)
        {
            return x;
        }

        x = IsPair(hand);
        if (x != 0)
        {
            return x;
        }

        x = IsHighCard(hand);
        return x;
    }

    public int IsRoyalFlush(List<Card> hand)
    {
        var royalValues = new HashSet<int> { 10, 11, 12, 13, 14 };
        var groups = hand.GroupBy(c => c.Suit);
        foreach (var group in groups)
        {
            var values = new HashSet<int>(group.Select(c => c.Value));
            if (royalValues.IsSubsetOf(values))
            {
                return 9000000;
            }
        }

        return 0;
    }

    public int IsStraightFlush(List<Card> hand)
    {
        var groups = hand.GroupBy(c => c.Suit);
        foreach (var group in groups)
        {
            var x = IsStraight(group.ToList());
            if (x != 0) return 4000000 + x;
        }

        return 0;
    }

    public int IsFourOfAKind(List<Card> hand)
    {
        var groups = hand.GroupBy(c => c.Value);
        var four = groups.FirstOrDefault(g => g.Count() == 4);
        if (four == null)
            return 0;
        var fourValue = four.Key;
        var kicker = groups.Where(g => g.Count() != 4).Max(g => g.Key);
        return 7000000 + 50 * fourValue + kicker;
    }

    public int IsFullHouse(List<Card> hand)
    {
        var groups = hand.GroupBy(c => c.Value);
        var threeValue = 0;
        var pairValue = 0;
        foreach (var group in groups)
        {
            if (group.Count() == 3)
            {
                if (group.Key > threeValue)
                    threeValue = group.Key;
            }

            if (group.Count() >= 2)
            {
                if (group.Key > pairValue && group.Key != threeValue)
                    pairValue = group.Key;
            }
        }

        if (threeValue == 0 || pairValue == 0)
            return 0;
        return 6000000 + 50 * threeValue + pairValue;
    }

    public int IsFlush(List<Card> hand)
    {
        var flush = hand.GroupBy(c => c.Suit)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault(g => g.Count() > 4);
        if (flush == null) return 0;
        return 5000000 + IsHighCard(flush.ToList());
    }

    public int IsStraight(List<Card> hand)
    {
        var values = new SortedSet<int>(hand.Select(c => c.Value)).ToList();
        if (values.Count < 5)
            return 0;
        if (new[] { 14, 2, 3, 4, 5 }.All(values.Contains))
            return 4000005;
        var inRow = 0;
        var highest = 0;
        for (var i = 1; i < values.Count; i++)
        {
            if (values[i - 1] + 1 == values[i])
                inRow++;
            else
                inRow = 0;

            if (inRow >= 4)
                highest = values[i];
        }

        if (highest > 0)
            return 4000000 + highest;
        return 0;
    }

    public int IsThreeOfAKind(List<Card> hand)
    {
        var groups = hand.GroupBy(c => c.Value);
        var threeValue = 0;
        foreach (var group in groups)
        {
            if (group.Count() == 3 && group.Key > threeValue) threeValue = group.Key;
        }

        if (threeValue == 0)
            return 0;
        var kickers = hand.Where(g => g.Value != threeValue)
            .OrderByDescending(c => c.Value)
            .Take(2)
            .ToList();
        return 3000000 + 10000 * threeValue + IsHighCard(kickers);
    }

    public int IsTwoPair(List<Card> hand)
    {
        var groups = hand.GroupBy(c => c.Value);
        groups = groups.OrderByDescending(g => g.Key).ToList();
        var pairHigh = 0;
        var pairLow = 0;
        foreach (var group in groups)
        {
            if (group.Count() == 2 && pairHigh == 0) pairHigh = group.Key;
            else if (group.Count() == 2) pairLow = group.Key;
        }

        if (pairHigh == 0 || pairLow == 0) return 0;
        return 2000000 + 10000 * pairHigh + 1000 * pairLow + IsHighCard(
            hand.Where(g => g.Value != pairHigh && g.Value != pairLow)
                .OrderByDescending(c => c.Value)
                .Take(1)
                .ToList()
        );
    }

    public int IsPair(List<Card> hand)
    {
        var groups = hand.GroupBy(c => c.Value);
        groups = groups.OrderByDescending(g => g.Key).ToList();
        var pairHigh = groups.FirstOrDefault(g => g.Count() == 2)?.Key ?? 0;
        if (pairHigh == 0) return 0;
        return 1000000 + 10000 * pairHigh + IsHighCard(
            hand.Where(g => g.Value != pairHigh)
                .OrderByDescending(c => c.Value)
                .Take(3)
                .ToList()
        );
    }

    public int IsHighCard(List<Card> hand)
    {
        var score = 0;
        var multiplier = 1;
        var orderedHand = hand.OrderByDescending(c => c.Value).Take(5);
        orderedHand = orderedHand.OrderBy(c => c.Value);
        foreach (var card in orderedHand)
        {
            score += card.Value * multiplier;
            multiplier *= 10;
        }

        return score;
    }
}