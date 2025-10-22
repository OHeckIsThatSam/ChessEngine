namespace ChessEngine.Core;

public static class FENUtil
{
    public const string STARTING_FEN_STRING = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    /// <summary>
    /// Decodes the FEN string into it's components populating the position struct
    /// with the data.
    /// More on FEN strings: https://en.wikipedia.org/wiki/Forsyth%E2%80%93Edwards_Notation
    /// </summary>
    /// <param name="FENString"></param>
    public static PositionAttributes FENToPosition(string FENString)
    {
        // Split the FEN string into it's 6 parts
        string[] FENParts = FENString.Split(' ');

        // Log if FEN string is valid if not default to starting position
        if (FENParts.Length != 6)
        {
            // TODO: Logging
            Console.WriteLine("Invalid FEN String, it did not contain exactly 6 parts.");
            FENParts = STARTING_FEN_STRING.Split(" ");
        }

        int[] boardSquares = new int[64];

        // FEN string notates piece placement from left to right, Bitboards square
        // index is reversed compared to FEN string. So we reverse FEN string and
        // board squares to get correct translation
        int rank = 7;
        int file = 7;

        char[] piecePlacement = FENParts[0].ToArray();
        Array.Reverse(piecePlacement);
        foreach (char symbol in new string(piecePlacement))
        {
            if ('/'.Equals(symbol))
            {
                rank--;
                file = 7;
                continue;
            }

            if (char.IsDigit(symbol))
            {
                file -= Convert.ToInt32(symbol.ToString());
                continue;
            }

            int squareIndex = rank * 8 + file;
            int pieceColour = char.IsUpper(symbol) ? Piece.White : Piece.Black;
            int piece = char.ToLower(symbol) switch
            {
                'k' => Piece.King,
                'q' => Piece.Queen,
                'r' => Piece.Rook,
                'b' => Piece.Bishop,
                'n' => Piece.Knight,
                'p' => Piece.Pawn,
                _ => Piece.None
            };

            boardSquares[squareIndex] = piece | pieceColour;
            file--;
        }

        int colourToMove = FENParts[1] == "w" ? Piece.White : Piece.Black;

        bool canWhiteKingSideCastle = FENParts[2].Contains('K');
        bool canWhiteQueenSideCastle = FENParts[2].Contains('Q');
        bool canBlackKingSideCastle = FENParts[2].Contains('k');
        bool canBlackQueenSideCastle = FENParts[2].Contains('q');

        int enPassantTargetSquare = 0;
        bool hasEnPassantTargetSquare = false;

        if (Enum.TryParse(FENParts[3], out BitboardUtil.Squares square))
        {
            enPassantTargetSquare = (int)square;
            hasEnPassantTargetSquare = true;
        }

        int fiftyMoveCount = Convert.ToInt32(FENParts[4]);

        int fullMoveCount = Convert.ToInt32(FENParts[5]);

        return new PositionAttributes(
            boardSquares, colourToMove,
            canWhiteKingSideCastle,
            canWhiteQueenSideCastle,
            canBlackKingSideCastle,
            canBlackQueenSideCastle,
            enPassantTargetSquare,
            hasEnPassantTargetSquare,
            fiftyMoveCount,
            fullMoveCount);
    }

    public readonly struct PositionAttributes(
        int[] boardSqaure,
        int colorToMove,
        bool canWhiteKingSideCastle,
        bool canWhiteQueenSideCastle,
        bool canBlackKingSideCastle,
        bool canBlackQueenSideCastle,
        int enPassantTargetSquare,
        bool hasEnPassantTargetSquare,
        int fiftyMoveCount, 
        int fullMoveCount)
    {
        public readonly int[] BoardSquares = boardSqaure;

        public readonly int ColourToMove = colorToMove;

        public readonly bool CanWhiteKingSideCastle = canWhiteKingSideCastle;
        public readonly bool CanWhiteQueenSideCastle = canWhiteQueenSideCastle;
        public readonly bool CanBlackKingSideCastle = canBlackKingSideCastle;
        public readonly bool CanBlackQueenSideCastle = canBlackQueenSideCastle;

        public readonly int EnPassantTargetSquare = enPassantTargetSquare;
        public readonly bool HasEnPassantTargetSquare = hasEnPassantTargetSquare;

        public readonly int FiftyMoveCount = fiftyMoveCount;
        public readonly int FullMoveCount = fullMoveCount;
    }
}
