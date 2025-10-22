namespace ChessEngine.Core;

public static class Piece
{
    public const int White = 0;
    public const int Black = 8;

    public const int None = 0;

    public const int Pawn = 1;
    public const int Knight = 2;
    public const int Bishop = 3;
    public const int Rook = 4;
    public const int Queen = 5;
    public const int King = 6;

    public const int WhitePawn = Pawn | White;
    public const int WhiteKnight = Knight | White;
    public const int WhiteBishop = Bishop | White;
    public const int WhiteRook = Rook | White;
    public const int WhiteQueen = Queen | White;
    public const int WhiteKing = King | White;

    public const int BlackPawn = Pawn | Black;
    public const int BlackKnight = Knight | Black;
    public const int BlackBishop = Bishop | Black;
    public const int BlackRook = Rook | Black;
    public const int BlackQueen = Queen | Black;
    public const int BlackKing = King | Black;

    public static int[] AllPieceTypes =
    [
        WhitePawn, WhiteKnight, WhiteBishop, WhiteRook, WhiteQueen, WhiteKing,
        BlackPawn, BlackKnight, BlackBishop, BlackRook, BlackQueen, BlackKing
    ];

    public static int[] PromotionTypes =
    [
        Knight, Bishop, Rook, Queen
    ];

    // Bit masks to get the type or colour since white is 0 and black is 8
    // the colour can be determined by if the 4 bit is on the remaining bits
    // denote type
    public const int TypeMask = 0b_0111;
    public const int ColourMask = 0b_1000;

    public static int GetPieceType(int piece) => piece & TypeMask;
    public static int GetPieceColour(int piece) => piece & ColourMask;

    public static bool IsSlider(int piece)
    {
        return GetPieceType(piece) switch
        {
            Bishop => true,
            Rook => true,
            Queen => true,
            _ => false,
        };
    }
}
