using Poker;

Table table = new Table(1000);
table.AddPlayer(new Player("Marek", table.buyIn));
table.AddPlayer(new Player("Szparek", table.buyIn));
table.AddPlayer(new Player("Darek", table.buyIn));
table.PlayRound();
table.PlayRound();
table.PlayRound();