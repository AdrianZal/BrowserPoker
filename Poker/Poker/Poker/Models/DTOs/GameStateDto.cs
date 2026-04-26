namespace Poker.Models.DTOs
{
    public class GameStateDto
    {
        public List<Game.Player> PlayersInGame { get; set; } = new();
        public List<Game.Player> Players { get; set; } = new();
        public Game.Table.GameStage Stage { get; set; }
        public List<Game.Card>? TableCards { get; set; }
        public Dictionary<string, int> PlayerBets { get; set; } = new();
        public int Pot { get; set; }
        public int ToCall { get; set; }
        public int MinRaise { get; set; }
        public Game.Player? CurrentPlayer { get; set; }
        public Dictionary<string, int> HandWinners { get; set; } = new();
        public string WinningHand { get; set; } = string.Empty;
        public Dictionary<string, Game.Table.PlayerRole> Roles { get; set; } = new();
        public Dictionary<string, Game.Table.PlayerStatus> Statuses { get; set; } = new();
        public bool IsLocked { get; set; }
    }
}
