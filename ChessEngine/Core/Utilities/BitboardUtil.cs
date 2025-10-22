using System.Numerics;
using System.Text;

namespace ChessEngine.Core;

public static class BitboardUtil
{
    public enum Squares
    {
        a8, b8, c8, d8, e8, f8, g8, h8,
        a7, b7, c7, d7, e7, f7, g7, h7,
        a6, b6, c6, d6, e6, f6, g6, h6,
        a5, b5, c5, d5, e5, f5, g5, h5,
        a4, b4, c4, d4, e4, f4, g4, h4,
        a3, b3, c3, d3, e3, f3, g3, h3,
        a2, b2, c2, d2, e2, f2, g2, h2,
        a1, b1, c1, d1, e1, f1, g1, h1
    }

    public static ulong GetBit(ulong bitboard, int squareIndex)
    {
        return bitboard & 1UL << squareIndex;
    }

    public static ulong AddBit(ulong bitboard, int squareIndex) 
    {
        return bitboard |= 1UL << squareIndex;
    }

    public static ulong RemoveBit(ulong bitboard, int squareIndex)
    {
        if (GetBit(bitboard, squareIndex) != 0)
            bitboard ^= 1UL << squareIndex;

        return bitboard;
    }

    public static int Count(ulong bitboard) => BitOperations.PopCount(bitboard);

    /// <summary>
    /// Returns the index of the smallest active bit.
    /// </summary>
    /// <returns>Index of the smallest active bit.</returns>
    public static int GetLeastSignificantBit(ulong bitboard)
    {
        return BitOperations.TrailingZeroCount(bitboard);
    }

    /// <summary>
    /// Pops the smallest bit from the bitboard while returning it's index.
    /// </summary>
    /// <param name="bitboard">A refrence of the bitboard</param>
    /// <returns>The index of the popped bit.</returns>
    public static int PopLSB(ref ulong bitboard)
    {
        int lsb = BitOperations.TrailingZeroCount(bitboard);
        bitboard ^= 1UL << lsb;
        return lsb;
    }

    /// <summary>
    /// Returns an array of all positions with an active bit within the 
    /// Bitboard. 
    /// Loops through by position from smallest to largest.
    /// </summary>
    /// <returns>The indexes of active bits.</returns>
    public static int[] GetActiveBits(ulong bitboard)
    {
        ulong bitboardCopy = bitboard;
        int length = Count(bitboard);
        int[] results = new int[length];

        for (int i = 0; i < length; i++)
        {
            // Get smallest index of active bit
            results[i] = BitOperations.TrailingZeroCount(bitboardCopy);

            // Remove bit from bitboard
            bitboardCopy ^= 1UL << results[i];
        }

        return results;
    }

    public static string ToString(ulong bitboard)
    {
        StringBuilder stringBuilder = new();

        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 0; file < 8; file++)
            {
                int squareIndex = rank * 8 + file;

                // Print rank number (descending)
                if (0.Equals(file))
                    stringBuilder.Append($" {8 - rank} ");

                // Convert the square index to determine if occupied 
                var occupied = GetBit(bitboard, squareIndex).Equals(0) ? 0 : 1;

                stringBuilder.Append($" {occupied}");
            }

            stringBuilder.AppendLine();
        }

        // Print files
        stringBuilder.AppendLine("\n    a b c d e f g h");

        // Print bit board decimal number
        stringBuilder.AppendLine($"\n    Bitboard: {bitboard}");

        return stringBuilder.ToString();
    }

    /*
     * Pre generated masks for bitboards. If we mask a piece Bitboard with a mask
     * and the resulting bitboard is some value other than 0 (not empty). Then we
     * can determine if the piece was within the area of the board that the mask 
     * represents.
     */
    public const ulong NotAFileMask = 18374403900871474942;
    public const ulong NotABFileMask = 18229723555195321596;
    public const ulong NotHFileMask = 9187201950435737471;
    public const ulong NotGHFileMask = 4557430888798830399;

    public const ulong Rank8Mask = 255;
    public const ulong Rank7Mask = 65280;
    public const ulong Rank2Mask = 71776119061217280;
    public const ulong Rank1Mask = 18374686479671623680;

    public const ulong WhiteKingSideCastleMask = 6917529027641081856;
    public const ulong WhiteQueenSideCastleMask = 864691128455135232;
    public const ulong WhiteQueenSideCastleBlockMask = 1008806316530991104;
    public const ulong BlackKingSideCastleMask = 96;
    public const ulong BlackQueenSideCastleMask = 12;
    public const ulong BlackQueenSideCastleBlockMask = 14;

    /*
     * Integers for the index change of piece moves/attacks. Values are used by 
     * shifting the bitboard with the pieces position.
     */
    public const int PawnForward = 8;

}
