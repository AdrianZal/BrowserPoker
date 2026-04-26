using System.Diagnostics.Metrics;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Poker.Models;
using Poker.Models.DTOs;
using Poker.Services;

namespace Poker.Game;

public class Table
{
    public enum PlayerRole
    {
        None,
        Dealer,
        SmallBlind,
        BigBlind
    }
    public enum PlayerStatus
    {
        Folded,
        ToCall,
        ToCheck,
        Checked,
        AllIn,
        Showing
    }
    public enum Decision
    {
        Fold,
        Call,
        Raise,
        Show,
        Tip
    }
    public enum GameStage 
    { 
        NotPlayable,
        NotStarted,
        PreFlop, 
        Flop, 
        Turn, 
        River, 
        GameOver 
    }

    public event Action<string> OnGameMessage;
    public event Action<GameStateDto> OnGameStateChanged;
    public event Action<Player> OnPlayerTurn;

    private readonly object _lock = new object();

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHandEvaluator _handEvaluator;

    private System.Timers.Timer? _turnTimer;
    private DateTime _turnEndTime;
    private const int TurnDurationSeconds = 15;

    public GameStage CurrentStage { get; private set; }
    public Player? CurrentPlayer { get; private set; }
    private int _currentPlayerIndex;

    public List<Player> playersInGame = new(7);
    public List<Player> players = new(7);
    private Deck deck;
    private List<Card> cards = new(5);
    public int buyIn;
    public string joinCode;
    private int smallBlind;
    private int dealerIndex;
    private int smallBlindIndex;
    private int bigBlindIndex;
    private int minRaise;
    private int toCall;
    private bool restartingHand = false;
    Dictionary<Player, int> roundBets = new();
    Dictionary<Player, int> handBets = new();
    Dictionary<Player, int> playerTips = new();
    Dictionary<Player, PlayerRole> playerRoles = new();
    Dictionary<Player, PlayerStatus> playerStatuses = new();
    Dictionary<Player, int> handWinners = new();
    private string winningHandDescription = string.Empty;
    private bool isLocked;

    public Table(int buyIn, string joinCode, IServiceScopeFactory scopeFactory, IHandEvaluator handEvaluator)
    {
        deck = new Deck();
        this.buyIn = buyIn;
        this.joinCode = joinCode;
        smallBlind = int.Max(buyIn / 100 / 2, 1);
        minRaise = smallBlind * 2;
        this.joinCode = joinCode;
        CurrentStage = GameStage.NotPlayable;
        _scopeFactory = scopeFactory;
        _handEvaluator = handEvaluator;
        _turnTimer = new System.Timers.Timer(TurnDurationSeconds * 1000);
        _turnTimer.AutoReset = false;
        _turnTimer.Elapsed += async (s, e) => await OnTurnTimeout();
        isLocked = false;
    }

    private void StartTimer()
    {
        _turnTimer?.Stop();
        _turnEndTime = DateTime.UtcNow.AddSeconds(TurnDurationSeconds);
        _turnTimer?.Start();
    }

    private void StopTimer()
    {
        _turnTimer?.Stop();
    }

    private async Task OnTurnTimeout()
    {
        if (CurrentPlayer == null) return;

        var canCheck = (toCall - roundBets[CurrentPlayer]) == 0;
        var decision = canCheck ? Decision.Call : Decision.Fold;

        PlayerAction(CurrentPlayer, decision, 0);
    }

    public void PlayHand()
    {
        lock (_lock)
        {
            players.RemoveAll(p => p.tableBalance <= 0);
            playersInGame = players.ToList();

            if (playersInGame.Count() < 2)
            {
                cards.Clear();
                handBets.Clear();
                roundBets.Clear();
                playerRoles.Clear();
                playerStatuses.Clear();
                handWinners.Clear();
                CurrentPlayer = null;
                winningHandDescription = string.Empty;
                foreach (var p in playersInGame)
                {
                    p.cards.Clear();
                }
                CurrentStage = GameStage.NotPlayable;
                NotifyStateUpdate();
                return;
            }

            CurrentStage = GameStage.PreFlop;
            Deal();
            BettingRoundReset(true);

            _currentPlayerIndex = playersInGame.Count == 2 ? smallBlindIndex : NextIndex(bigBlindIndex);
            CurrentPlayer = playersInGame[_currentPlayerIndex];

            StartTimer();

            NotifyStateUpdate();
        }
    }

    public void PlayerAction(Player player, Decision decision, int amount = 0)
    {
        lock (_lock)
        {
            if (decision == Decision.Tip && player.tableBalance > 0)
            {
                player.tableBalance -= 1;
                playerTips[player] += 1;
                NotifyStateUpdate();
                return;
            }

            if (player != CurrentPlayer && decision != Decision.Show)
            {
                OnGameMessage?.Invoke("It is not your turn.");
                return;
            }

            StopTimer();

            ProcessDecision(player, decision, amount);

            if (decision != Decision.Show)
            {
                AdvanceGame();
                if (CurrentStage == GameStage.GameOver)
                {
                    restartingHand = true;
                    _ = DelayAndPlayHand();
                }
            }
            NotifyStateUpdate();
        }
    }

    private async Task DelayAndPlayHand()
    {
        await Task.Delay(8000);
        restartingHand = false;
        PlayHand();
    }

    private void ProcessDecision(Player player, Decision decision, int amount)
    {
        switch (decision)
        {
            case Decision.Show:
                if (playerStatuses[player] == PlayerStatus.Folded)
                {
                    playerStatuses[player] = PlayerStatus.Showing;
                }
                break;
            case Decision.Fold:
                playerStatuses[player] = PlayerStatus.Folded;
                break;
            case Decision.Call:
                var callAmount = toCall - roundBets[player];
                if (player.tableBalance <= callAmount)
                {
                    HandleBet(player, player.tableBalance);
                    playerStatuses[player] = PlayerStatus.AllIn;
                }
                else
                {
                    HandleBet(player, callAmount);
                    playerStatuses[player] = PlayerStatus.Checked;
                }
                break;
            case Decision.Raise:

                if (amount > player.tableBalance)
                {
                    playerStatuses[player] = PlayerStatus.Folded;
                    break;
                }

                if (amount < minRaise && amount == player.tableBalance)
                {
                    HandleBet(player, amount);
                    toCall = roundBets.Values.Max();
                    playerStatuses[player] = PlayerStatus.AllIn;
                    break;
                }

                if(amount > minRaise)
                {
                    minRaise = amount;
                }

                if (playerStatuses[player] == PlayerStatus.ToCall && amount + (toCall - roundBets[player]) >= player.tableBalance)
                {
                    HandleBet(player, player.tableBalance);
                }
                else if (playerStatuses[player] == PlayerStatus.ToCall)
                {
                    HandleBet(player, amount + (toCall - roundBets[player]));
                }
                else
                {
                    HandleBet(player, amount);
                }
                toCall = roundBets.Values.Max();

                if (player.tableBalance == 0)
                    playerStatuses[player] = PlayerStatus.AllIn;
                else
                    playerStatuses[player] = PlayerStatus.Checked;
                foreach (var p in playersInGame)
                {
                    if (p != player && playerStatuses[p] != PlayerStatus.Folded &&
                        playerStatuses[p] != PlayerStatus.AllIn)
                    {
                        playerStatuses[p] = PlayerStatus.ToCall;
                    }
                }
                break;
        }
    }

    private void AdvanceGame()
    {
        if (playerStatuses.Count(s => s.Value != PlayerStatus.Folded) == 1)
        {
            CurrentStage = GameStage.GameOver;
            handWinners = ResolveRound(true);
            NotifyStateUpdate();
            return;
        }

        if (IsBettingRoundComplete())
        {
            do
            {
                NextStage();
                NotifyStateUpdate();
            } while ((!someoneCanAct() || oneMustCheck()) && CurrentStage != GameStage.GameOver);
        }
        else
        {
            if (!someoneCanAct())
            {
                NextStage();
                return;
            }

            do
            {
                _currentPlayerIndex = NextIndex(_currentPlayerIndex);
                CurrentPlayer = playersInGame[_currentPlayerIndex];
            } while (playerStatuses[CurrentPlayer] == PlayerStatus.Folded || playerStatuses[CurrentPlayer] == PlayerStatus.AllIn);

            StartTimer();
        }
        NotifyStateUpdate();
    }

    private bool someoneCanAct() 
    {
        return playerStatuses.Any(s => s.Value != PlayerStatus.Folded && s.Value != PlayerStatus.AllIn); 
    }
    private bool oneMustCheck()
    {
        return playerStatuses.Count(s => s.Value == PlayerStatus.ToCheck) == 1;
    }
    private bool IsBettingRoundComplete()
    {
        return playerStatuses.Count(s => s.Value == PlayerStatus.ToCall || s.Value == PlayerStatus.ToCheck) == 0;
    }

    private void NextStage()
    {
        switch (CurrentStage)
        {
            case GameStage.PreFlop:
                Flop();
                CurrentStage = GameStage.Flop;
                break;
            case GameStage.Flop:
                DrawTableCard();
                CurrentStage = GameStage.Turn;
                break;
            case GameStage.Turn:
                DrawTableCard();
                CurrentStage = GameStage.River;
                break;
            case GameStage.River:
                handWinners = ResolveRound(false);
                CurrentStage = GameStage.GameOver;
                break;
        }

        if (CurrentStage != GameStage.GameOver)
        {
            BettingRoundReset(false);
            _currentPlayerIndex = playersInGame.Count == 2 ? bigBlindIndex : smallBlindIndex;
            CurrentPlayer = playersInGame[_currentPlayerIndex];

            while ((playerStatuses[CurrentPlayer] == PlayerStatus.Folded
                || playerStatuses[CurrentPlayer] == PlayerStatus.AllIn)
                && someoneCanAct())
            {
                _currentPlayerIndex = NextIndex(_currentPlayerIndex);
                CurrentPlayer = playersInGame[_currentPlayerIndex];
            }
            StartTimer();
        }
    }

    // Helper to send data out
    public void NotifyStateUpdate()
    {
        lock (_lock)
        {
            // 1. Określamy listę graczy wysyłaną do klienta
            var activeList = (CurrentStage == GameStage.NotStarted || CurrentStage == GameStage.NotPlayable)
                             ? players : playersInGame;

            var dto = new GameStateDto
            {
                PlayersInGame = activeList.ToList(),
                Players = players,
                Stage = CurrentStage,
                TableCards = cards.ToList(),

                // 2. FILTRUJEMY SŁOWNIKI: Tylko gracze z listy activeList trafiają do DTO
                // Zapobiega to kolizji kluczy (imion) ze "starych" obiektów graczy
                PlayerBets = roundBets.Where(kv => activeList.Contains(kv.Key))
                                      .ToDictionary(kv => kv.Key.name, kv => kv.Value),

                Statuses = playerStatuses.Where(kv => activeList.Contains(kv.Key))
                                         .ToDictionary(kv => kv.Key.name, kv => kv.Value),

                Roles = playerRoles.Where(kv => activeList.Contains(kv.Key))
                                   .ToDictionary(kv => kv.Key.name, kv => kv.Value),

                Pot = handBets.Values.Sum(),
                ToCall = toCall,
                MinRaise = minRaise,
                CurrentPlayer = CurrentPlayer,
                HandWinners = handWinners.ToDictionary(k => k.Key.name, v => v.Value),
                WinningHand = winningHandDescription,
                IsLocked = isLocked
            };
            OnGameStateChanged?.Invoke(dto);
        }
    }

    private void HandleBet(Player player, int amount)
    {
        roundBets[player] += amount;
        handBets[player] += amount;
        player.tableBalance -= amount;
    }

    private Dictionary<Player, int> ResolveRound(bool endedEarly)
    {
        CurrentPlayer = null;

        Dictionary<Player, int> winningsByPlayer = new();
        Dictionary<Player, int> playerScores = new();
        if (endedEarly)
        {
           playerScores = playersInGame.Where(p => playerStatuses[p] != PlayerStatus.Folded).ToDictionary(p => p, p => 0);
        }
        else
        {
            playerScores = playersInGame.Where(p => playerStatuses[p] != PlayerStatus.Folded).ToDictionary(p => p, p => _handEvaluator.Evaluate7(p.cards.Concat(cards).ToList()));
        }
        
        int maxScore = playerScores.Values.Max();

        var levels = handBets.Values.Where(v => v > 0).Distinct().OrderBy(v => v).ToList();
        int prev = 0;

        foreach (var level in levels)
        {
            int contribution = level - prev;
            
            var contributors = playersInGame.Where(p => handBets[p] >= level);
            int potAmount = contributors.Count() * contribution;

            var eligible = playersInGame
                .Where(p => handBets[p] >= level && playerStatuses[p] != PlayerStatus.Folded)
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

        if (playerScores.Count() >= 2)
        {
            winningHandDescription = WhatHand(maxScore);
            foreach (var p in playersInGame)
            {
                if (playerStatuses[p] != PlayerStatus.Folded)
                {
                    playerStatuses[p] = PlayerStatus.Showing;
                }
            }
        }
        else
        {
            foreach (var p in playersInGame)
            {
                if (playerStatuses[p] != PlayerStatus.Folded)
                {
                    playerStatuses[p] = PlayerStatus.Folded;
                }
            }
        }
        _ = CaseLottery(winningsByPlayer);
        return winningsByPlayer;
    }

    private async Task CaseLottery(Dictionary<Player, int> winners)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var utilService = scope.ServiceProvider.GetRequiredService<UtilService>();

            foreach (var winner in winners)
            {
                if (RandomNumberGenerator.GetInt32(10000) < (100 + playerTips[winner.Key] * 10))
                {
                    await utilService.AddCaseToPlayer(winner.Key.name);
                    playerTips[winner.Key] = 0;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lottery error: {ex.Message}");
        }
    }

    private void Reindex()
    {
        dealerIndex = NextIndex(dealerIndex);
        if (playersInGame.Count == 2)
        {
            smallBlindIndex = dealerIndex;
            bigBlindIndex = NextIndex(dealerIndex);
        }
        else if (playersInGame.Count >= 3)
        {
            smallBlindIndex = NextIndex(dealerIndex);
            bigBlindIndex = NextIndex(smallBlindIndex);
        }
        
        playerRoles.Clear();
        for (var i = 0; i < playersInGame.Count; i++)
        {
            var player = playersInGame[i];
            playerRoles[player] =
                i == dealerIndex ? PlayerRole.Dealer :
                i == smallBlindIndex ? PlayerRole.SmallBlind :
                i == bigBlindIndex ? PlayerRole.BigBlind :
                PlayerRole.None;
        }
    }

    private int NextIndex(int currentIndex)
    {
        return (currentIndex + 1) % playersInGame.Count;
    }

    private void BettingRoundReset(bool preflop)
    {
        roundBets.Clear();
        if (preflop)
        {
            winningHandDescription = string.Empty;
            handBets.Clear();
            handWinners.Clear();
            Reindex();
        }
        else
        {
            toCall =0 ;
        }
        minRaise = smallBlind * 2;
        foreach (var player in playersInGame)
        {
            roundBets.Add(player, 0);
            if (preflop)
            {
                handBets.Add(player, 0);
                if (playersInGame.Count() == 2)
                {
                    switch (playerRoles[player])
                    {
                        case PlayerRole.Dealer:
                            if(player.tableBalance <= smallBlind)
                            {
                                HandleBet(player, player.tableBalance);
                                playerStatuses[player] = PlayerStatus.AllIn;
                            }
                            else
                            {
                                HandleBet(player, smallBlind);
                                playerStatuses[player] = PlayerStatus.ToCall;
                            }
                            break;
                        case PlayerRole.BigBlind:
                            if (player.tableBalance <= smallBlind * 2)
                            {
                                HandleBet(player, player.tableBalance);
                                playerStatuses[player] = PlayerStatus.AllIn;
                            }
                            else
                            {
                                HandleBet(player, smallBlind * 2);
                                playerStatuses[player] = PlayerStatus.ToCheck;
                            }
                            break;
                    }
                }
                else
                {
                    switch (playerRoles[player])
                    {
                        case PlayerRole.SmallBlind:
                            if (player.tableBalance <= smallBlind)
                            {
                                HandleBet(player, player.tableBalance);
                                playerStatuses[player] = PlayerStatus.AllIn;
                            }
                            else
                            {
                                HandleBet(player, smallBlind);
                                playerStatuses[player] = PlayerStatus.ToCall;
                            }
                            break;
                        case PlayerRole.BigBlind:
                            if (player.tableBalance <= smallBlind * 2)
                            {
                                HandleBet(player, player.tableBalance);
                                playerStatuses[player] = PlayerStatus.AllIn;
                            }
                            else
                            {
                                HandleBet(player, smallBlind * 2);
                                playerStatuses[player] = PlayerStatus.ToCheck;
                            }
                            break;
                        default:
                            handBets[player] = 0;
                            playerStatuses[player] = PlayerStatus.ToCall;
                            break;
                    }
                }
            }
            else
            {
                if (playerStatuses[player] != PlayerStatus.Folded && playerStatuses[player] != PlayerStatus.AllIn)
                    playerStatuses[player] = PlayerStatus.ToCheck;
            }
        }
        toCall = roundBets.Values.Max();
    }

    public async Task AddPlayer(Player player)
    {
        lock (_lock)
        {
            if (players.Count >= 7 || players.Any(p => p.name == player.name))
                return;
            players.Add(player);
            if (players.Count == 2 && CurrentStage == GameStage.NotPlayable)
            {
                CurrentStage = GameStage.NotStarted;
            }
            playerTips[player] = 0;
            NotifyStateUpdate();
        }
    }

    public async Task RemovePlayer(Player player)
    {
        lock (_lock)
        {
            if (player == null || !players.Contains(player))
            {
                return;
            }
            if (CurrentStage != GameStage.NotStarted && CurrentStage != GameStage.NotPlayable)
            {
                if (CurrentPlayer == player)
                {
                    players.Remove(player);
                    PlayerAction(player, Decision.Fold, 0);
                }
                else if (CurrentStage != GameStage.GameOver)
                {
                    playerStatuses[player] = PlayerStatus.Folded;
                }
            }
            if (players.Contains(player))
            {
                players.Remove(player);
            }
            if (players.Count() < 2 && CurrentStage == GameStage.NotStarted)
            {
                CurrentStage = GameStage.NotPlayable;
            }
            playerTips.Remove(player);
            NotifyStateUpdate();
        }
    }

    public bool IsLocked()
    {
        return isLocked;
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;
        NotifyStateUpdate();
    }

    public bool IsFull()
    {
        return playersInGame.Count >= 7;
    }

    private void Deal()
    {
        cards.Clear();
        deck.Shuffle();
        foreach (var player in playersInGame)
        {
            player.cards.Clear();
        }

        for (var i = 0; i < 2; i++)
        {
            foreach (var p in playersInGame)
            {
                p.cards.Add(deck.Draw());
            }
        }
    }

    private void Flop()
    {
        for (var i = 0; i < 3; i++)
            DrawTableCard();
    }

    private void DrawTableCard()
    {
        cards.Add(deck.Draw());
    }

    private string WhatHand(int x)
    {
        if (x == 7462) return "Royal flush";
        if (x >= 7453) return "Straight flush";
        if (x >= 7297) return "Four of a kind";
        if (x >= 7141) return "Full house";
        if (x >= 5864) return "Flush";
        if (x >= 5854) return "Straight";
        if (x >= 4996) return "Three of a kind";
        if (x >= 4138) return "Two pair";
        if (x >= 1278) return "Pair";
        if (x > 0) return "High card";

        return "Invalid hand";
    }

}