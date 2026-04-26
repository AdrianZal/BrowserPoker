using Microsoft.AspNetCore.SignalR;
using Poker.Game;
using PokerServer.Hubs;

public class GameService(IHubContext<PokerHub> hubContext, IServiceScopeFactory scopeFactory, IHandEvaluator handEvaluator)
{
    private readonly Dictionary<string, Table> _tables = new();

    public string CreateTable(int buyIn)
    {
        string code;

        do
        {
            code = Random.Shared.Next(0, 1000000).ToString("D6");
        } while (_tables.ContainsKey(code));

        var table = new Table(buyIn ,code, scopeFactory, handEvaluator);

        table.OnGameStateChanged += async (state) =>
        {
            await hubContext.Clients.Group(code).SendAsync("UpdateState", state);
        };

        _tables.Add(code, table);
        return code;
    }

    public Table? GetTable(string code) => _tables.GetValueOrDefault(code);

    public Table? GetTableByPlayer(string playerName)
    {
        return _tables.Values.FirstOrDefault(table =>
            table.players.Any(p => p.name == playerName));
    }

    public void RemoveTable(string code)
    {
        if (_tables.ContainsKey(code))
        {
            _tables.Remove(code);
        }
    }
}