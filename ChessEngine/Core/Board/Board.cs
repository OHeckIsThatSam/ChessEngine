namespace ChessEngine.Core;

public class Board : ICloneable
{
    /// <summary>
    /// Represents all Pieces on the board. Each position in the array is a square
    /// on the board, and the integer representing the Piece occupying it.
    /// </summary>
    public int[] BoardSquares = new int[64];

    /// <summary>
    /// Array of all piece bitboards with position corresponding to Piece's number
    /// </summary>
    public ulong[] PieceBitboards = new ulong[15];

    public ulong OccupiedBitboard = 0;
    public ulong EmptyBitboard => ~OccupiedBitboard;
    public ulong[] ColourAttacksBitboard = new ulong[2];

    public bool IsCheck;
    public bool IsCheckmate;
    public bool IsStalemate;

    public int ColourToMove;
    public int OppositionColour => ColourToMove == Piece.White ? Piece.Black : Piece.White;

    public int CastlingRights => CurrentBoardState.CastlingRights;
    public bool CanWhiteKingSideCastle => (CastlingRights & 0b_1000) != 0;
    public bool CanWhiteQueenSideCastle => (CastlingRights & 0b_0100) != 0;
    public bool CanBlackKingSideCastle => (CastlingRights & 0b_0010) != 0;
    public bool CanBlackQueenSideCastle => (CastlingRights & 0b_0001) != 0;

    public bool HasEnPassantTargetSquare => CurrentBoardState.EnPassantSquare != 0;
    public int EnPassantTargetSquare => CurrentBoardState.EnPassantSquare;

    public BoardState CurrentBoardState;
    public List<Move> moveHistory = [];

    private Stack<BoardState> _previousStates = [];
    private int _fiftyMoveCount;
    private int _plyCount;

    public void SetPosition(string FENString)
    {
        var position = FENUtil.FENToPosition(FENString);

        BoardSquares = position.BoardSquares;

        InitialisePieceBitboards();

        ColourToMove = position.ColourToMove;

        // Create int representing castling options
        int castlingRights = (position.CanWhiteKingSideCastle ? 1 << 3 : 0)
            | (position.CanWhiteQueenSideCastle ? 1 << 2 : 0)
            | (position.CanBlackKingSideCastle ? 1 << 1 : 0)
            | (position.CanBlackQueenSideCastle ? 1 << 0 : 0);

        // Create new board state required for hash calculation
        CurrentBoardState = new(
            Piece.None,
            castlingRights,
            position.EnPassantTargetSquare,
            position.FiftyMoveCount,
            0);
        ulong zobristHash = Zobrist.InitialPositionToHash(this);
        CurrentBoardState = new(
            Piece.None,
            castlingRights,
            position.EnPassantTargetSquare,
            position.FiftyMoveCount,
            zobristHash);

        _fiftyMoveCount = position.FiftyMoveCount;
        _plyCount = (position.FullMoveCount * 2) + (ColourToMove == Piece.Black ? 1 : 0);

        UpdateCheck();

        _previousStates.Push(CurrentBoardState);
    }

    private void InitialisePieceBitboards()
    {
        for (int i = 0; i < PieceBitboards.Length; i++)
            PieceBitboards[i] = new();

        for (int squareIndex = 0; squareIndex < BoardSquares.Length; squareIndex++)
        {
            if (BoardSquares[squareIndex] == 0)
                continue;

            int piece = BoardSquares[squareIndex];
            int colour = Piece.GetPieceColour(BoardSquares[squareIndex]);

            // Put type into own bitboard
            PieceBitboards[piece] = BitboardUtil.AddBit(
                PieceBitboards[piece], squareIndex);

            // Put colour into own bitboard
            PieceBitboards[colour] = BitboardUtil.AddBit(
                PieceBitboards[colour], squareIndex);

            OccupiedBitboard = BitboardUtil.AddBit(OccupiedBitboard, squareIndex);
        }
    }

    public void MakeMove(Move move)
    {
        // Get piece from the start square
        int piece = BoardSquares[move.StartSquare];
        ulong pieceBitboard = PieceBitboards[piece];
        ulong zobristHash = CurrentBoardState.ZobristHash;

        // Pick up piece on all board representations
        BoardSquares[move.StartSquare] = 0;
        OccupiedBitboard = BitboardUtil.RemoveBit(OccupiedBitboard, move.StartSquare);
        pieceBitboard = BitboardUtil.RemoveBit(pieceBitboard, move.StartSquare);
        zobristHash ^= Zobrist.Pieces[piece, move.StartSquare];

        // Remove captured piece or enPassant pawn from bitboard
        int capturedPiece = Piece.None;
        if (move.IsEnPassant)
        {
            capturedPiece = BoardSquares[move.TargetPawnSquare];
            PieceBitboards[capturedPiece] = BitboardUtil.RemoveBit(
                PieceBitboards[capturedPiece], move.TargetPawnSquare);
            BoardSquares[move.TargetPawnSquare] = 0;
            OccupiedBitboard = BitboardUtil.RemoveBit(OccupiedBitboard, move.TargetPawnSquare);
            zobristHash ^= Zobrist.Pieces[capturedPiece, move.TargetPawnSquare];
        }
        else if (move.IsCapture)
        {
            capturedPiece = BoardSquares[move.TargetSquare];
            PieceBitboards[capturedPiece] = BitboardUtil.RemoveBit(
                PieceBitboards[capturedPiece], move.TargetSquare);
            zobristHash ^= Zobrist.Pieces[capturedPiece, move.TargetSquare];
        }

        if (move.IsPromotion)
        {
            // Swap pawn bitboard for promotion type bitboard
            PieceBitboards[piece] = pieceBitboard;
            piece = move.PromotionType | ColourToMove;
            pieceBitboard = PieceBitboards[piece];
        }

        // Make move on all board representations
        BoardSquares[move.TargetSquare] = piece;
        OccupiedBitboard = BitboardUtil.AddBit(OccupiedBitboard, move.TargetSquare);
        pieceBitboard = BitboardUtil.AddBit(pieceBitboard, move.TargetSquare);
        zobristHash ^= Zobrist.Pieces[piece, move.TargetSquare];

        PieceBitboards[piece] = pieceBitboard;

        // Move rook if castling
        if (move.IsCastling)
        {
            int rook = BoardSquares[move.RookStartSquare];
            ulong rookBitboard = PieceBitboards[rook];

            BoardSquares[move.RookStartSquare] = 0;
            OccupiedBitboard = BitboardUtil.RemoveBit(
                OccupiedBitboard, move.RookStartSquare);
            rookBitboard = BitboardUtil.RemoveBit(rookBitboard, move.RookStartSquare);

            BoardSquares[move.RookTargetSquare] = rook;
            OccupiedBitboard = BitboardUtil.AddBit(
                OccupiedBitboard, move.RookTargetSquare);
            rookBitboard = BitboardUtil.AddBit(rookBitboard, move.RookTargetSquare);

            PieceBitboards[rook] = rookBitboard;
            zobristHash ^= Zobrist.Pieces[rook, move.RookStartSquare];
            zobristHash ^= Zobrist.Pieces[rook, move.RookTargetSquare];
        }

        // Update castling rights
        int updatedCastlingRights = CurrentBoardState.CastlingRights;
        if (updatedCastlingRights != 0)
        {
            if ((Piece.TypeMask & piece) == Piece.King || move.IsCastling)
            {
                int colourMask = ColourToMove == Piece.White ? 0b_0011 : 0b_1100;
                // Remove castling rights for that side
                updatedCastlingRights &= colourMask;
            }
            else
            {
                if (move.StartSquare == (int)BitboardUtil.Squares.h1 ||
                    move.TargetSquare == (int)BitboardUtil.Squares.h1)
                {
                    updatedCastlingRights &= 0b_0111;
                }
                if (move.StartSquare == (int)BitboardUtil.Squares.a1 ||
                    move.TargetSquare == (int)BitboardUtil.Squares.a1)
                {
                    updatedCastlingRights &= 0b_1011;
                }
                if (move.StartSquare == (int)BitboardUtil.Squares.h8 ||
                    move.TargetSquare == (int)BitboardUtil.Squares.h8)
                {
                    updatedCastlingRights &= 0b_1101;
                }
                if (move.StartSquare == (int)BitboardUtil.Squares.a8 ||
                    move.TargetSquare == (int)BitboardUtil.Squares.a8)
                {
                    updatedCastlingRights &= 0b_1110;
                }
            }
        }

        // Negate side to move and prev EnPassant for next position hash
        zobristHash ^= Zobrist.BlackToMove;
        zobristHash ^= Zobrist.EnPassantFiles[
            BoardUtil.SquareToFile(CurrentBoardState.EnPassantSquare)];
        
        if (CurrentBoardState.CastlingRights != updatedCastlingRights)
        {
            zobristHash ^= Zobrist.CastlingRights[CurrentBoardState.CastlingRights];
            zobristHash ^= Zobrist.CastlingRights[updatedCastlingRights];
        }

        ToggleColourToMove();
        UpdateColourBitboards();
        UpdateCheck();

        _plyCount++;
        int updatedFiftyMoveCounter = CurrentBoardState.FiftyMoveCount + 1;
        
        // If move was pawn or capture then stop fifty move count or repetition 
        

        moveHistory.Add(move);

        // Create new state
        CurrentBoardState = new(
            capturedPiece,
            updatedCastlingRights,
            move.EnPassantTargetSquare,
            updatedFiftyMoveCounter,
            zobristHash);
        _previousStates.Push(CurrentBoardState);
    }

    public void ReverseMove(Move move)
    {
        // Reverse rook move if castle
        if (move.IsCastling)
        {
            int rook = BoardSquares[move.RookTargetSquare];
            ulong rookBitboard = PieceBitboards[rook];

            BoardSquares[move.RookTargetSquare] = 0;
            OccupiedBitboard = BitboardUtil.RemoveBit(
                OccupiedBitboard, move.RookTargetSquare);
            rookBitboard = BitboardUtil.RemoveBit(rookBitboard, move.RookTargetSquare);

            BoardSquares[move.RookStartSquare] = rook;
            OccupiedBitboard = BitboardUtil.AddBit(
                OccupiedBitboard, move.RookStartSquare);
            rookBitboard = BitboardUtil.AddBit(rookBitboard, move.RookStartSquare);

            PieceBitboards[rook] = rookBitboard;
        }

        int piece = BoardSquares[move.TargetSquare];
        ulong pieceBitboard = PieceBitboards[piece];

        BoardSquares[move.TargetSquare] = 0;
        OccupiedBitboard = BitboardUtil.RemoveBit(
            OccupiedBitboard, move.TargetSquare);
        pieceBitboard = BitboardUtil.RemoveBit(pieceBitboard, move.TargetSquare);

        PieceBitboards[piece] = pieceBitboard;

        if (move.IsCapture)
        {
            int capturedPiece = move.CapturedPiece;
            int capturedPieceSquare = move.TargetSquare;

            if (move.IsEnPassant)
                capturedPieceSquare = move.TargetPawnSquare;

            // Add captured piece back to boards
            BoardSquares[capturedPieceSquare] = capturedPiece;
            OccupiedBitboard = BitboardUtil.AddBit(
                OccupiedBitboard, capturedPieceSquare);
            PieceBitboards[capturedPiece] = BitboardUtil.AddBit(
                PieceBitboards[capturedPiece], capturedPieceSquare);
        }

        if (move.IsPromotion)
        {
            piece = Piece.Pawn | OppositionColour;
            pieceBitboard = PieceBitboards[piece];
        }

        // Add moved piece back to original position
        BoardSquares[move.StartSquare] = piece;
        OccupiedBitboard = BitboardUtil.AddBit(OccupiedBitboard, move.StartSquare);
        PieceBitboards[piece] = BitboardUtil.AddBit(pieceBitboard, move.StartSquare);

        ToggleColourToMove();
        UpdateColourBitboards();
        UpdateCheck();

        // Decrement move count
        _plyCount--;

        moveHistory.RemoveAt(moveHistory.Count - 1);

        _previousStates.Pop();
        CurrentBoardState = _previousStates.Peek();
    }

    private void ToggleColourToMove() => ColourToMove = OppositionColour;

    private void UpdateColourBitboards()
    {
        PieceBitboards[Piece.White] =
            PieceBitboards[Piece.WhitePawn] |
            PieceBitboards[Piece.WhiteKnight] |
            PieceBitboards[Piece.WhiteBishop] |
            PieceBitboards[Piece.WhiteRook] |
            PieceBitboards[Piece.WhiteQueen] |
            PieceBitboards[Piece.WhiteKing];

        PieceBitboards[Piece.Black] =
            PieceBitboards[Piece.BlackPawn] |
            PieceBitboards[Piece.BlackKnight] |
            PieceBitboards[Piece.BlackBishop] |
            PieceBitboards[Piece.BlackRook] |
            PieceBitboards[Piece.BlackQueen] |
            PieceBitboards[Piece.BlackKing];
    }

    private void UpdateCheck()
    {
        ulong oppAttacks = AttackBitboards.GetAllAttacks(
            OppositionColour, OccupiedBitboard, PieceBitboards);

        IsCheck = (oppAttacks & PieceBitboards[Piece.King | ColourToMove]) != 0;
    }

    public object Clone()
    {
        return new Board()
        {
            BoardSquares = (int[])BoardSquares.Clone(),
            PieceBitboards = (ulong[])PieceBitboards.Clone(),
            OccupiedBitboard = OccupiedBitboard,

            IsCheck = IsCheck,
            IsCheckmate = IsCheckmate,
            IsStalemate = IsStalemate,

            ColourToMove = ColourToMove,

            CurrentBoardState = CurrentBoardState,
            moveHistory = new List<Move>(moveHistory),

            _previousStates = new(_previousStates.Reverse()),
            _plyCount = _plyCount,
            _fiftyMoveCount = _fiftyMoveCount,
        };
    }
}
