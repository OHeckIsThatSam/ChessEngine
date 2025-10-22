using ChessEngine.Core;
using System.Diagnostics;

namespace ChessEngine.Testing;

public static class Perft
{
    private static readonly bool _outputStats = true;

    public static void Create(int depth, string fen, string setup_moves)
    {
        // Init pre-calculated move bitboards before any board representation
        AttackBitboards.Initialise();
        Magic.Initialise();

        // Create position from FEN string
        Board position = new();
        position.SetPosition(fen);

        // Make any moves prior to perft
        if (setup_moves != "")
        {
            string[] splitUciMoves = setup_moves.Split(' ');

            for (int i = 0; i < splitUciMoves.Length; i++)
                position.MakeMove(MoveUtil.UCIToMove(splitUciMoves[i], position));
        }

        var sw = Stopwatch.StartNew();
        var moves = MoveGeneration.GenerateMoves(position);
        
        long total = 0;
        for (int i = 0; i < moves.Count; i++)
        {
            var move = moves[i];

            position.MakeMove(move);
            long count = Perftree(position, depth - 1);

            position.ReverseMove(move);
            // Output move and number of perft states at that depth
            Console.WriteLine($"{MoveUtil.MoveToUCI(move)} {count}");
            total += count;
        }
        sw.Stop();

        // Output total positions count
        Console.WriteLine();
        Console.WriteLine(total);

        if (_outputStats)
        {
            long ms = sw.ElapsedMilliseconds;
            float seconds = (float)ms / 1000;
            float nps = float.Truncate(total / seconds);
            File.AppendAllText(
                "perftNPS.txt", 
                $"{fen}, {depth}, {ms}, {total}, {nps}\n");
        }
    }

    private static long Perftree(Board position, int depth)
    {
        var moves = MoveGeneration.GenerateMoves(position);

        if (depth <= 1)
        {
            return moves.Count;
        }

        long count = 0;
        for (int i = 0; i < moves.Count; i++)
        {
            var move = moves[i];

            position.MakeMove(move);
            count += Perftree(position, depth - 1);
            position.ReverseMove(move);
        }

        return count;
    }
}
