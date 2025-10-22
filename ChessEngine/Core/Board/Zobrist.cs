namespace ChessEngine.Core;

public static class Zobrist
{
    // Hashes for each piece on each square
    public static readonly ulong[,] Pieces = new ulong[15, 64];

    public static readonly ulong BlackToMove;

    public static readonly ulong[] CastlingRights = new ulong[16];

    public static readonly ulong[] EnPassantFiles = new ulong[8];

    static Zobrist()
    {
        Random rng = new();

        for (int squareIndex = 0; squareIndex < 64; squareIndex++)
        {
            for (int i = 0; i < Piece.AllPieceTypes.Length; i++)
            {
                int piece = Piece.AllPieceTypes[i];
                Pieces[piece, squareIndex] = GenerateRandomUlong(rng);
            }
        }

        BlackToMove = GenerateRandomUlong(rng);

        for (int i = 0; i < CastlingRights.Length; i++)
        {
            CastlingRights[i] = GenerateRandomUlong(rng);
        }

        for (int file = 0; file < EnPassantFiles.Length; file++)
        {
            EnPassantFiles[file] = GenerateRandomUlong(rng);
        }
    }

    public static ulong InitialPositionToHash(Board position)
    {
        ulong hash = 0;

        for (int squareIndex = 0; squareIndex < 64; squareIndex++)
        {
            int piece = position.BoardSquares[squareIndex];
            hash ^= Pieces[piece, squareIndex];
        }

        if (position.ColourToMove == Piece.Black)
            hash ^= BlackToMove;

        hash ^= CastlingRights[position.CastlingRights];

        if (position.HasEnPassantTargetSquare)
        {
            int file = BoardUtil.SquareToFile(position.EnPassantTargetSquare);
            hash ^= EnPassantFiles[file];
        }

        return hash;
    }

    private static ulong GenerateRandomUlong(Random rng)
    {
        byte[] next = new byte[8];
        rng.NextBytes(next);
        return BitConverter.ToUInt64(next);
    }
}
