using Microsoft.AspNetCore.SignalR;

public class User
{
    public string ConnectionId { get; set; }
    public string Name { get; set; }
    public string ProfilePicture { get; set; }
}



public class Image
{
    public string Name { get; set; }
    public string Url { get; set; }
    public string ProfilePicture { get; set; }
    //public DateTime Timestamp { get; set; }
}

public class Player
{
    private const int STEP = 5;
    private const int FINISH = 100;
    public string Id { get; set; }
    public string ProfilePicture { get; set; }
    public string Name { get; set; }
    public string Symbol { get; set; }
    public Player(string id, string profilePicture, string name) => (Id, ProfilePicture, Name) = (id, profilePicture, name);

    public int Count { get; set; } = 0;
    public bool IsWin => Count >= FINISH;

    public void Run()
    {
        Count += STEP;
        Console.WriteLine($"Player {Name} Count: {Count}");
    }

    public void ReduceCount(int value)
    {
        Count -= value;
        if (Count < 0) Count = 0; // Ensure count doesn't go below 0
        
    }
}

public class Game
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public Player? PlayerA { get; set; }
    public Player? PlayerB { get; set; }

    public bool IsWaiting { get; set; } = false;
    //public bool IsWaiting => PlayerA == null || PlayerB == null;
    public bool IsEmpty => PlayerA == null && PlayerB == null;
    public bool IsFull => PlayerA != null && PlayerB != null;

    public string? Type { get; set; } // Type of the game
    public string? AddPlayer(Player player)
    {
        if (PlayerA == null)
        {
            PlayerA = player;
            IsWaiting = true;
            return "A";
        }
        else if (PlayerB == null)
        {
            PlayerB = player;
            IsWaiting = false;
            return "B";
        }

        throw new InvalidOperationException("Game is full.");
        //return null;
    }
}

public class TicTacToe : Game
{
    public string CurrentTurn { get; private set; } = "A"; // Track whose turn it is, default to "A"

    public string[] Board { get; private set; } = new string[9]; // Game board (9 cells)

    public TicTacToe()
    {
        Type = "TicTacToe";
        Array.Fill(Board, ""); // Initialize the board with empty strings
    }
    public void ChangeTurn()
    {
        CurrentTurn = CurrentTurn == "A" ? "B" : "A";
    }

    public bool IsPlayerTurn(string role)
    {
        return CurrentTurn == role;
    }

    public bool CheckWin(string symbol)
    {
        // Winning combinations (indexes of the board array)
        int[][] winningCombinations = new[]
        {
            new[] { 0, 1, 2 }, // Top row
            new[] { 3, 4, 5 }, // Middle row
            new[] { 6, 7, 8 }, // Bottom row
            new[] { 0, 3, 6 }, // Left column
            new[] { 1, 4, 7 }, // Middle column
            new[] { 2, 5, 8 }, // Right column
            new[] { 0, 4, 8 }, // Diagonal
            new[] { 2, 4, 6 }  // Diagonal
        };

        return winningCombinations.Any(combination => combination.All(index => Board[index] == symbol));
    }

    public bool CheckDraw()
    {
        return Board.All(cell => cell == "X" || cell == "O") && !CheckWin("X") && !CheckWin("O");
    }
    public void UpdateBoard(int position, string symbol)
    {
        //|| Board[position] != ""
        if (position < 0 || position >= Board.Length)
        {
            throw new InvalidOperationException("Invalid move.");
        }

        Board[position] = symbol;
    }
}

public class RaceGame : Game
{
    public RaceGame()
    {
        Type = "RaceGame"; // Set the type in the constructor
    }


}

public class ChatHub : Hub
{
    // chat
    private static int count = 0;
    private static List<string> names = [];
    private static List<User> users = new List<User>();
    private static List<Image> images = new List<Image>();

    // Game
    //private static readonly Dictionary<string, Game> Games = new();

    private static List<Game> games = [];


    // ------------------------------------------------------------------------------------------
    // CHAT
    // ------------------------------------------------------------------------------------------
    public async Task SendText(string name, string message, string profilePicture)
    {
        await Clients.Caller.SendAsync("ReceiveText", name, message, profilePicture, "caller");
        await Clients.Others.SendAsync("ReceiveText", name, message, profilePicture, "others");
    }

    public async Task SendImage(string name, string url, string profilePicture)
    {
        await Clients.Caller.SendAsync("ReceiveImage", name, url, profilePicture, "caller");
        await Clients.Others.SendAsync("ReceiveImage", name, url, profilePicture, "others");
    }

    public async Task SendYouTube(string name, string id, string profilePicture)
    {
        await Clients.Caller.SendAsync("ReceiveYouTube", name, id, profilePicture, "caller");
        await Clients.Others.SendAsync("ReceiveYouTube", name, id, profilePicture, "others");
    }

    public async Task SendFile(string name, string url, string filename, string profilePicture)
    {
        await Clients.Caller.SendAsync("ReceiveFile", name, url, filename, profilePicture, "caller");
        await Clients.Others.SendAsync("ReceiveFile", name, url, filename, profilePicture, "others");
    }

    public async Task SendSticker(string name, string url, string profilePicture)
    {
        await Clients.Caller.SendAsync("ReceiveSticker", name, url, profilePicture, "caller");
        await Clients.Others.SendAsync("ReceiveSticker", name, url, profilePicture, "others");
    }


    public override async Task OnConnectedAsync()
    {
        count++;
        string name = Context.GetHttpContext().Request.Query["name"].ToString();
        string profilePicture = Context.GetHttpContext().Request.Query["profilePicture"].ToString();

        var user = new User
        {
            ConnectionId = Context.ConnectionId,
            Name = name,
            ProfilePicture = profilePicture
        };

        users.Add(user);

        string page = Context.GetHttpContext()!.Request.Query["page"].ToString();

        switch (page)
        {
            case "chat": await ListConnected(); break;
            case "ticTaeToe": await GameConnected(); break;
            case "raceGame": await GameConnected(); break;
        }


        await Clients.All.SendAsync("UpdateStatus", count, $"<b>{name}</b> joined", users);
        await base.OnConnectedAsync();
    }


    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        count--;
        var user = users.FirstOrDefault(u => u.ConnectionId == Context.ConnectionId);
        if (user != null)
        {
            users.Remove(user);
            await Clients.All.SendAsync("UpdateStatus", count, $"<b>{user.Name}</b> left", users);
        }

        string page = Context.GetHttpContext()!.Request.Query["page"].ToString();

        switch (page)
        {
            case "chat": await ListDisconnected(); break;
            case "ticTaeToe": await GameDisconnected(); break;
            case "raceGame": await GameDisconnected(); break;
        }

        await base.OnDisconnectedAsync(exception);
    }

    public Task<List<User>> GetActiveUsers()
    {
        return Task.FromResult(users);
    }

    public List<Image> GetImages()
    {
        return images;
    }



    // ------------------------------------------------------------------------------------------
    // GAME 
    // ------------------------------------------------------------------------------------------
    // create, do nothing
    public string Create(string gameType)
    {
        Game game;

        if (gameType == "TicTacToe")
        {
            game = new TicTacToe(); // Create a Tic Tac Toe game
        }
        else if (gameType == "RaceGame")
        {
            game = new RaceGame(); ; // Create a Race Game
        }
        else
        {
            throw new ArgumentException("Unknown game type.");
        }

        games.Add(game);
        return game.Id;
    }

    // Race Game
    public async Task Run(string role, bool isBombTriggered = false)
    {
        string gameId = Context.GetHttpContext()!.Request.Query["gameId"].ToString();

        var game = games.Find(g => g.Id == gameId);
        if (game == null)
        {
            await Clients.Caller.SendAsync("Reject");
            return;
        }

        var player = role == "A" ? game.PlayerA : game.PlayerB;
        var opponent = role == "A" ? game.PlayerB : game.PlayerA;
        if (player == null) return;

        
        if (isBombTriggered)
        {
            opponent.ReduceCount(1); // Reduce opponent's count by 5
            await Clients.Group(gameId).SendAsync("BombTriggered", role, opponent.Count);
        }
        else
        {
            player.Run();
            await Clients.Group(gameId).SendAsync("Move", role, player.Count);

            if (player.IsWin)
            {
                await Clients.Group(gameId).SendAsync("Win", role);
            }
        }
    }

    // TIC TAE TOE
    public async Task Turn(string role, int position)
    {
        string gameId = Context.GetHttpContext()!.Request.Query["gameId"].ToString();

        var game = games.Find(g => g.Id == gameId);
        if (game == null)
        {
            await Clients.Caller.SendAsync("Reject");
            return;
        }


        /// Perform additional checks based on the game type
        if (game is TicTacToe ticTacToe)
        {
            if (!ticTacToe.IsPlayerTurn(role))
            {
                await Clients.Caller.SendAsync("Error", "It's not your turn.");
                return;
            }

            var playerSymbol = role == "A" ? "X" : "O";
            // Notify clients about the new game state
            // Extract player data including profile pictures
            var player1 = game.PlayerA;
            var player2 = game.PlayerB;

            var player1Name = player1?.Name;
            var player1ProfilePicture = player1?.ProfilePicture;
            var player2Name = player2?.Name;
            var player2ProfilePicture = player2?.ProfilePicture;
            var currentTurn = ticTacToe.CurrentTurn;
            // Determine the current player's name
            var currentPlayerName = currentTurn == "A" ? player1Name : player2Name;
            try
            {
                // Check if the position is already occupied
                if (ticTacToe.Board[position] != "")
                {
                    // Notify clients that the move was invalid
                    await Clients.Caller.SendAsync("InvalidMove", "Invalid move: Position already occupied.");

                    return;
                }

                // Update the board with the player's move
                ticTacToe.UpdateBoard(position, playerSymbol);

                // Check for win or draw
                bool hasWon = ticTacToe.CheckWin(playerSymbol);
                bool isDraw = ticTacToe.CheckDraw();

                if (hasWon)
                {
                    await Clients.Group(gameId).SendAsync("UpdateBoard", ticTacToe.Board);
                    await Clients.Group(gameId).SendAsync("GameOver", $"{currentPlayerName} wins!");
                }
                else if (isDraw)
                {
                    await Clients.Group(gameId).SendAsync("UpdateBoard", ticTacToe.Board);
                    await Clients.Group(gameId).SendAsync("GameOver", "It's a draw!");
                }
                else
                {
                    // Change turn
                    ticTacToe.ChangeTurn();


                    // Send updated game state including board information
                    await Clients.Group(gameId).SendAsync("UpdateGame", ticTacToe.CurrentTurn, player1Name, player1ProfilePicture, player2Name, player2ProfilePicture);
                    await Clients.Group(gameId).SendAsync("UpdateBoard", ticTacToe.Board);

                }
            }
            catch (InvalidOperationException ex)
            {
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }
    }
    public async Task SendStickers(string role, string url)
    {
        await Clients.All.SendAsync("ReceiveStickers", role, url);
    }
    // ----------------------------------------------------------------------------------------
    // Connected
    // ----------------------------------------------------------------------------------------

    private async Task ListConnected()
    {
        await Clients.Caller.SendAsync("UpdateGameStatus", games.Where(g => g.IsWaiting).ToList());
    }

    private async Task GameConnected()
    {
        string id = Context.ConnectionId;
        string profilePicture = Context.GetHttpContext()!.Request.Query["profilePicture"].ToString();
        string name = Context.GetHttpContext()!.Request.Query["name"].ToString();
        string gameId = Context.GetHttpContext()!.Request.Query["gameId"].ToString();
        string gameType = Context.GetHttpContext()!.Request.Query["gameType"].ToString(); // Get game type

        // Check for an existing game by gameId
        var game = games.Find(g => g.Id == gameId);

        // If the game doesn't exist or is full, create a new game based on gameType
        if (game == null || game.IsFull)
        {
            await Clients.Caller.SendAsync("Reject");
            return;
        }


        // Join the existing game
        var player = new Player(id, profilePicture, name);
        var role = game.AddPlayer(player);

        await Clients.Caller.SendAsync("RoleAssigned", role);
        await Groups.AddToGroupAsync(id, gameId);


        // Notify players that the game is ready
        await Clients.Group(game.Id).SendAsync("Ready", role, game);

        await Clients.All.SendAsync("UpdateGameStatus", games.Where(g => g.IsWaiting).ToList());

        if (game.IsFull)
        {
            // Extract player data including profile pictures
            var player1 = game.PlayerA;
            var player2 = game.PlayerB;

            var player1Name = player1?.Name;
            var player1ProfilePicture = player1?.ProfilePicture;
            var player2Name = player2?.Name;
            var player2ProfilePicture = player2?.ProfilePicture;

            // Check if it's a TicTacToe game to get the current turn
            if (game is TicTacToe tttGame)
            {
                var currentTurn = tttGame.CurrentTurn;

                await Clients.Group(game.Id).SendAsync("Start");

                // Send 'UpdateGame' message with the game state
                await Clients.Group(game.Id).SendAsync("UpdateGame", currentTurn, player1Name, player1ProfilePicture, player2Name, player2ProfilePicture);
            }
            else if (game is RaceGame)
            {
                // Handle RaceGame specific logic here if needed
                await Clients.Group(game.Id).SendAsync("Start");
            }
        }
    }

    // ----------------------------------------------------------------------------------------
    // Disconnected
    // ---------------------------------------------------------------------------------------

    private async Task ListDisconnected()
    {
        await Task.CompletedTask;
    }



    private async Task GameDisconnected()
    {
        string id = Context.ConnectionId;
        string gameId = Context.GetHttpContext()!.Request.Query["gameId"].ToString();

        var game = games.Find(g => g.Id == gameId);
        var player1 = game.PlayerA;
        var player2 = game.PlayerB;

        var player1Name = player1?.Name;
        var player2Name = player2?.Name;
        if (game == null)
        {
            return;
        }

        if (game.PlayerA?.Id == id)
        {
            game.PlayerA = null;
            await Clients.Group(gameId).SendAsync("Left", player1Name);
        }
        else if (game.PlayerB?.Id == id)
        {
            game.PlayerB = null;
            await Clients.Group(gameId).SendAsync("Left", player2Name);
        }

        if (game.IsEmpty)
        {
            games.Remove(game);
            await Clients.All.SendAsync("UpdateGameStatus", games.Where(g => g.IsWaiting).ToList());
        }
    }

}