﻿namespace ChessEngine.Core;

public class Search(int maxDepth)
{
    public Move BestMove = new();

    public int maxSearchTime;
    public int maxDepth = maxDepth;

    public static int SearchMoves(Board position, int depth)
    {
        if (0.Equals(depth))
        {
            return Evaluation.Evaluate(position);
        }

        int score = int.MinValue;

        foreach (Move move in MoveGeneration.GenerateMoves(position))
        {
            position.MakeMove(move);

            int currentScore = -SearchMoves(position, depth - 1);

            if (currentScore > score)
                score = currentScore;

            position.ReverseMove(move);
        }

        return score;
    }
}
