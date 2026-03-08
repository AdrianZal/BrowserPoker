namespace Poker.Models
{
    public class PlayerCases
    {
        public int PlayerId { get; set; }
        public int Number { get; set; }

        public virtual Player Player { get; set; } = null!;
    }
}
