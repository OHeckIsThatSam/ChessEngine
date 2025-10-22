using ChessEngine.Core;
using System.Diagnostics;
using System.Text;

namespace ChessEngine.Testing;

internal class ComparePositions
{
    public static void Compare(Board before, Move move, Board after)
    {
        bool isUnequal = false;

        StringBuilder sb = new();
        sb.AppendLine("Position Comparison\n");

        sb.AppendLine("Move Made:");
        sb.AppendLine($"Starting square: {(BitboardUtil.Squares)move.StartSquare}");
        sb.AppendLine($"Target square: {(BitboardUtil.Squares)move.TargetSquare}");

        sb.AppendLine($"Is Capture: {move.IsCapture}");
        sb.AppendLine($"Captured Piece: {move.CapturedPiece}");

        sb.AppendLine($"Is EnPassant: {move.IsEnPassant}");
        sb.AppendLine($"TargetPawnSquare: {(BitboardUtil.Squares)move.TargetPawnSquare}");

        sb.AppendLine($"Is Castling: {move.IsCastling}");
        sb.AppendLine($"Rook Start Square: {(BitboardUtil.Squares)move.RookStartSquare}");
        sb.AppendLine($"Rook Target Square: {(BitboardUtil.Squares)move.RookTargetSquare}");

        sb.AppendLine($"Is Promotion: {move.IsPromotion}");
        sb.AppendLine($"Promotion Type: {move.PromotionType}");

        sb.AppendLine("\nBoard Squares: ");
        bool boardSquareUnequal = false;
        // Compare piece positions
        for (int i = 0; i < before.BoardSquares.Length; i++)
        {
            if (before.BoardSquares[i] != after.BoardSquares[i])
            {
                boardSquareUnequal = true;
                isUnequal = true;
                sb.Append($"Unequal square: {(BitboardUtil.Squares)i} ");
            }
        }

        if (boardSquareUnequal)
        {
            sb.AppendLine("Before:");
            sb.AppendLine(BoardUtil.BoardToString(before));
            sb.AppendLine("After:");
            sb.AppendLine(BoardUtil.BoardToString(after));
        }

        for (int i = 0; i < before.PieceBitboards.Length; i++)
        {
            ulong b = before.PieceBitboards[i];
            ulong a = after.PieceBitboards[i];
            if (a != b)
            {
                sb.AppendLine($"Unequal bitboard: {i}");
                sb.AppendLine($"Before:\n{BitboardUtil.ToString(b)}");
                sb.AppendLine($"After:\n{BitboardUtil.ToString(a)}");
            }
        }

        sb.AppendLine($"{(before.IsCheck == after.IsCheck ? "Equal Check" : "UnEqual Check")}");
        sb.AppendLine($"{(before.IsCheckmate == after.IsCheckmate ? "Equal Checkmate" : "UnEqual Checkmate")}");
        sb.AppendLine($"{(before.IsStalemate == after.IsStalemate ? "Equal Stalemate" : "UnEqual Stalemate")}");

        sb.AppendLine($"{(before.HasEnPassantTargetSquare == after.HasEnPassantTargetSquare? "Equal Has EnPassantTargetSquare" : "UnEqual Has EnPassantTargetSquare")}");
        if (before.HasEnPassantTargetSquare)
        {
            sb.AppendLine($"Before: {before.EnPassantTargetSquare}, After: {after.EnPassantTargetSquare}");
        }

        if (before.CurrentBoardState.ZobristHash != after.CurrentBoardState.ZobristHash)
        {
            isUnequal = true;
            sb.AppendLine($"Zobrist before: {before.CurrentBoardState.ZobristHash}, After:{after.CurrentBoardState.ZobristHash}");
            sb.AppendLine($"Piece Type before: {before.CurrentBoardState.CapturedPieceType}, After:{after.CurrentBoardState.CapturedPieceType}");
            sb.AppendLine($"Castling before: {before.CurrentBoardState.CastlingRights}, After:{after.CurrentBoardState.CastlingRights}");
            sb.AppendLine($"EnPassant before: {before.CurrentBoardState.EnPassantSquare}, After:{after.CurrentBoardState.EnPassantSquare}");
            sb.AppendLine($"FiftyMove before: {before.CurrentBoardState.FiftyMoveCount}, After:{after.CurrentBoardState.FiftyMoveCount}");
        }

        if (isUnequal)
        {
            File.WriteAllText("dump.txt", sb.ToString());
            Debug.Assert(isUnequal == false, "Position is unequal, see debug dump file.");
        }
    }
}
