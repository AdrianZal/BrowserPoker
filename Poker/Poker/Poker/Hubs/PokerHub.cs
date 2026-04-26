using System.Numerics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Poker.Game;
using Poker.Models;


namespace PokerServer.Hubs
{
    [Authorize]
    public class PokerHub(PokerContext context, GameService gameService) : Hub
    {
        public async Task JoinTable(string joinCode)
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int playerId))
            {
                throw new HubException("User identity not found.");
            }

            var userEquippedSkin = await context.PlayerEquippedReverseSkins
                .Where(x => x.PlayerId == playerId)
                .Select(x => x.PlayerOwnedReverseSkin.Skin.Filename)
                .FirstOrDefaultAsync();
            if (userEquippedSkin == null) 
            {
                throw new HubException("Equipped skin not found.");
            }

            var playerInfo = await context.Players.FindAsync(playerId);
            if (playerInfo == null) throw new HubException("Player profile not found.");

            var table = gameService.GetTable(joinCode);
            if (table == null) throw new HubException("Table not found.");
            if (table.IsLocked()) throw new HubException("Table locked.");

            await Groups.AddToGroupAsync(Context.ConnectionId, joinCode);

            var existingPlayer = table.players.FirstOrDefault(p => p.name == playerInfo.Name);

            if (existingPlayer == null)
            {
                if (table.IsFull()) throw new HubException("Table is full.");

                if (table.buyIn > playerInfo.Balance)
                    throw new HubException("Insufficient balance to join the table.");

                playerInfo.Balance -= table.buyIn;
                try
                {
                    await context.SaveChangesAsync();
                }
                catch
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, joinCode);
                    throw new HubException("Transaction failed.");
                }
                var player = new Poker.Game.Player(playerInfo.Name, table.buyIn, userEquippedSkin);

                await table.AddPlayer(player);

                await Clients.Group(joinCode).SendAsync("PlayerJoined", player.name);
            }
            else
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, joinCode);
            }
        }

        public async Task LeaveTable(string joinCode)
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int playerId))
            {
                throw new HubException("User identity not found.");
            }

            var table = gameService.GetTable(joinCode);
            if (table == null)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, joinCode);
                return;
            }

            var playerInfo = await context.Players.FindAsync(playerId);
            if (playerInfo == null) throw new HubException("Player profile not found.");

            var tablePlayer = table.players.FirstOrDefault(p => p.name == playerInfo.Name);

            if (tablePlayer != null)
            {
                playerInfo.Balance += tablePlayer.tableBalance;

                await table.RemovePlayer(tablePlayer);

                await context.SaveChangesAsync();

                await Clients.Group(joinCode).SendAsync("PlayerLeft", tablePlayer.name);

                if (table.players.Count == 0)
                {
                    gameService.RemoveTable(joinCode);
                }
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, joinCode);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int playerId))
            {
                var playerInfo = await context.Players.FindAsync(playerId);
                if (playerInfo != null)
                {
                    var table = gameService.GetTableByPlayer(playerInfo.Name);

                    if (table != null)
                    {
                        var tablePlayer = table.players.FirstOrDefault(p => p.name == playerInfo.Name);
                        if (tablePlayer != null)
                        {
                            playerInfo.Balance += tablePlayer.tableBalance;
                            await table.RemovePlayer(tablePlayer);
                            await context.SaveChangesAsync();

                            await Clients.Group(table.joinCode).SendAsync("PlayerLeft", tablePlayer.name);
                        }
                        if (table.players.Count == 0)
                        {
                            gameService.RemoveTable(table.joinCode);
                        }
                    }
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        public void SendAction(string playerName, Poker.Game.Table.Decision decision, int amount)
        {
            var table = gameService.GetTableByPlayer(playerName);
            if (table == null) throw new HubException("Table not found.");
            table.PlayerAction(
                table.players.First(p => p.name == playerName),
                decision,
                amount
            );
            if(decision == Poker.Game.Table.Decision.Tip)
            {
                Clients.Group(table.joinCode).SendAsync("DealerTipped");
            }
        }

        public void SendMessage(string playerName, string message)
        {
            var table = gameService.GetTableByPlayer(playerName);
            if (table == null) throw new HubException("Table not found.");
            var wholeMessage = playerName + ": " + message;
            Clients.Group(table.joinCode).SendAsync("Message", wholeMessage);
        }

        public void SetLock(string playerName, bool locked)
        {
            var table = gameService.GetTableByPlayer(playerName);
            if (table == null) throw new HubException("Table not found.");
            table.SetLocked(locked);
            Clients.Group(table.joinCode).SendAsync("TableLock", table.IsLocked());
        }

        public void StartGame(string joinCode)
        {
            var table = gameService.GetTable(joinCode);
            if (table == null) throw new HubException("Table not found.");
            table.PlayHand();
        }

    }
}
