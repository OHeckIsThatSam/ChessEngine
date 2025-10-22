namespace ChessEngine.Core;

public readonly struct BoardState(
    int capturedPieceType,
    int castlingRights,
    int enPassantSquare,
    int fiftyMoveCount,
    ulong zobristHash)
{
    public readonly int CapturedPieceType = capturedPieceType;
    public readonly int CastlingRights = castlingRights;
    public readonly int EnPassantSquare = enPassantSquare;
    public readonly int FiftyMoveCount = fiftyMoveCount;
    public readonly ulong ZobristHash = zobristHash;
}
