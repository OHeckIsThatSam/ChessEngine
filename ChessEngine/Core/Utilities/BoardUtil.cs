using System.Text;

namespace ChessEngine.Core;

public static class BoardUtil
{
    public static int SquareToFile(int square)
    {
        return square & 0b_000111;
    }

    public static int SquareToRank(int square)
    {
        return square >> 3;
    }

    public static string BoardToString(Board position)
    {
        StringBuilder sb = new();

        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 0; file < 8; file++)
            {
                int squareIndex = rank * 8 + file;

                // Print rank number (descending)
                if (0.Equals(file))
                    sb.Append($" {8 - rank} ");

                string piece = position.BoardSquares[squareIndex] switch
                {
                    Piece.WhitePawn => "P",
                    Piece.BlackPawn => "p",
                    Piece.WhiteKnight => "N",
                    Piece.BlackKnight => "n",
                    Piece.WhiteBishop => "B",
                    Piece.BlackBishop => "b",
                    Piece.WhiteRook => "R",
                    Piece.BlackRook => "r",
                    Piece.WhiteQueen => "Q",
                    Piece.BlackQueen => "q",
                    Piece.WhiteKing => "K",
                    Piece.BlackKing => "k",
                    _ => "."
                };

                sb.Append($"{piece} ");
            }

            sb.AppendLine();
        }

        // Print files
        sb.AppendLine("\n   a b c d e f g h");

        return sb.ToString();
    }
}
