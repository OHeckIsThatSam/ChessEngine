using ChessEngine.Core;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ChessEngine.Testing;

static class Test
{
    static private Dictionary<int, int> _nodeCount = [];
    static private List<int[]> _searchStats = [new int[7]];

    public static void TestMoveGeneration()
    {
        string path = Directory.GetCurrentDirectory();
        path += @"\Testing\Positions\testPositions.txt";

        string jsonText = File.ReadAllText(path);

        var json = JsonSerializer.Deserialize<JsonArray>(jsonText);

        foreach (var item in json)
        {
            _searchStats = [];

            int targetDepth = Convert.ToInt32(item["depth"].ToString());
            long targetNodes = Convert.ToInt64(item["nodes"].ToString());
            string fen = item["fen"].ToString();

            for (int i = 0; i < targetDepth; i++)
                _searchStats.Add(new int[7]);   

            Console.WriteLine("Creating game...");

            Board board = new();
            board.SetPosition(fen);

            Console.WriteLine($"Position: {fen}\n");

            Console.WriteLine(BoardUtil.BoardToString(board));

            // Build move tree to analyse accuracy of moves generated
            PerftStats(board, 0, targetDepth);

            // Time the search function
            Stopwatch stopwatch = Stopwatch.StartNew();
            Search.SearchMoves(board, targetDepth);
            stopwatch.Stop();

            Console.WriteLine("Node Tree Breakdown:");
            Console.WriteLine("Depth\tNodes\tCaptures\tE.p.\tCastles\tPromotions\tChecks\tCheckmates");
            for (int i = 0; i < _searchStats.Count; i++)
            {
                int[] stats = _searchStats[i];
                Console.WriteLine($"{i + 1}\t{stats[0]}\t{stats[1]}\t\t{stats[2]}\t{stats[3]}\t{stats[4]}\t\t{stats[5]}\t{stats[6]}");
            }
            Console.WriteLine();

            int actualNodeCount = _searchStats[targetDepth - 1][0];
            double timeTaken = stopwatch.Elapsed.TotalSeconds;
            double nodesPerSeconds = actualNodeCount / timeTaken;

            Console.WriteLine($"Seconds: {timeTaken}");
            Console.WriteLine($"Total Nodes: {actualNodeCount}");
            Console.WriteLine($"NPS: {nodesPerSeconds}\n");

            Console.WriteLine($"Test result: {(actualNodeCount == targetNodes ? "SUCCESS" : "FAILURE")}");
            Console.WriteLine($"Expected nodes count: {targetNodes} - Actual node count: {actualNodeCount}\n");

            Console.WriteLine("Moves generated. Enter to proceed... ");
            Console.ReadLine();
            Thread.Sleep(500);
            Console.Clear();
        }
    }

    // Simple perft search that aggregates extra stats from a position
    private static void PerftStats(Board position, int depth, int max_depth)
    {
        if (depth == max_depth)
            return;

        var moves = MoveGeneration.GenerateMoves(position);
        _searchStats[depth][0] += moves.Count;
        for (int i = 0; i < moves.Count; i++)
        {
            var move = moves[i];
            Board before = (Board)position.Clone();

            if (move.IsCapture)
                _searchStats[depth][1] += 1;
            if (move.IsEnPassant)
                _searchStats[depth][2] += 1;
            if (move.IsCastling)
                _searchStats[depth][3] += 1;
            if (move.IsPromotion)
                _searchStats[depth][4] += 1;

            position.MakeMove(move);
            
            if (position.IsCheck)
            {
                _searchStats[depth][5] += 1;

                // Check for checkmate
                if (MoveGeneration.GenerateMoves(position).Count == 0)
                    _searchStats[depth][6] += 1;
            }
            
            PerftStats(position, depth + 1, max_depth);

            position.ReverseMove(move);

            ComparePositions.Compare(before, move, position);
        }
    }
}
