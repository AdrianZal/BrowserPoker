using System;
using System.Collections.Generic;

namespace Poker.Models;

public partial class Player
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public int Balance { get; set; }

    public virtual ICollection<PlayerOwnedFaceSkin> PlayerOwnedFaceSkins { get; set; } = new List<PlayerOwnedFaceSkin>();

    public virtual ICollection<PlayerOwnedReverseSkin> PlayerOwnedReverseSkins { get; set; } = new List<PlayerOwnedReverseSkin>();

    public virtual PlayerTable? PlayerTable { get; set; }

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual PlayerCases? PlayerCases { get; set; }
}
