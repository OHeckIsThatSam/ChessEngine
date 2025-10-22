using ChessEngine.Core;
using System.Diagnostics;

namespace ChessEngine;

public class Engine
{
    private Board _board;

    public Engine()
    { 
        _board = new Board();
    }

    public void CreateGame(string FENString = "")
    {
        if (string.IsNullOrEmpty(FENString))
            FENString = FENUtil.STARTING_FEN_STRING;

        _board.SetPosition(FENString);

        //Search search = new();
        Stopwatch.StartNew();
        MoveGeneration.GenerateMoves(_board);
        Console.WriteLine($"Time taken to gen moves: {Stopwatch.GetElapsedTime(Stopwatch.GetTimestamp())}");
        

        Console.WriteLine(Evaluation.Evaluate(_board));
    }
}
