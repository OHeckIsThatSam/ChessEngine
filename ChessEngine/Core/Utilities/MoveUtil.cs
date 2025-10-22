namespace ChessEngine.Core;

public static class MoveUtil
{
    public static string MoveToUCI(Move move)
    {
        string startSquare = ((BitboardUtil.Squares)move.StartSquare).ToString();

        string uciMove = startSquare + ((BitboardUtil.Squares)move.TargetSquare).ToString();

        // Append promotion type
        if (move.IsPromotion)
        {
            uciMove += move.PromotionType switch
            {
                Piece.Knight => "n",
                Piece.Bishop => "b",
                Piece.Rook => "r",
                Piece.Queen => "q",
                _ => ""
            };
        }

        return uciMove;
    }

    public static Move UCIToMove(string move, Board position)
    {
        int startSquare = (int)Enum.Parse(
            typeof(BitboardUtil.Squares), move.Substring(0, 2).AsSpan());

        int targetSquare = (int)Enum.Parse(
            typeof(BitboardUtil.Squares), move.Substring(2, 2).AsSpan());

        int movedPiece = position.BoardSquares[startSquare];
        bool isCapture = position.BoardSquares[targetSquare] != Piece.None;

        bool isPromotion = false;
        int promotionType = Piece.None;
        bool hasEnPassantTarget = false;
        int enPassantTargetSquare = 0;
        bool isEnPassant = false;
        int targetPawnSquare = 0;
        if (Piece.Pawn == Piece.GetPieceType(movedPiece))
        {
            int rankMoveDifference = Math.Abs(BoardUtil.SquareToRank(startSquare) -
                BoardUtil.SquareToRank(targetSquare));

            if (move.Length > 4)
            {
                isPromotion = true;
                promotionType = move[4] switch
                {
                    'q' => Piece.Queen,
                    'r' => Piece.Rook,
                    'b' => Piece.Bishop,
                    'k' => Piece.Knight,
                    _ => Piece.None
                };
            }
            else if (BoardUtil.SquareToFile(startSquare) != BoardUtil.SquareToFile(targetSquare) &&
                !isCapture)
            {
                isCapture = true;
                isEnPassant = true;
                targetPawnSquare = targetSquare +
                    (Piece.GetPieceColour(movedPiece) == Piece.White ? 8 : -8);
            }
            else if (rankMoveDifference == 2)
            {
                hasEnPassantTarget = true;
                enPassantTargetSquare = targetSquare +
                    (Piece.GetPieceColour(movedPiece) == Piece.White ? 8 : -8);
            }
        }

        bool isCastling = false;
        int rookStartSquare = 0;
        int rookTargetSquare = 0;
        if (Piece.King == Piece.GetPieceType(movedPiece))
        {
            if (startSquare == (int)BitboardUtil.Squares.e1 &&
                targetSquare == (int)BitboardUtil.Squares.g1)
            {
                isCastling = true;
                rookStartSquare = (int)BitboardUtil.Squares.h1;
                rookTargetSquare = (int)BitboardUtil.Squares.f1;
            }
            else if (startSquare == (int)BitboardUtil.Squares.e1 &&
                targetSquare == (int)BitboardUtil.Squares.c1)
            {
                isCastling = true;
                rookStartSquare = (int)BitboardUtil.Squares.a1;
                rookTargetSquare = (int)BitboardUtil.Squares.d1;
            }
            else if (startSquare == (int)BitboardUtil.Squares.e8 &&
                targetSquare == (int)BitboardUtil.Squares.g8)
            {
                isCastling = true;
                rookStartSquare = (int)BitboardUtil.Squares.h8;
                rookTargetSquare = (int)BitboardUtil.Squares.f8;
            }
            else if (startSquare == (int)BitboardUtil.Squares.e8 &&
                targetSquare == (int)BitboardUtil.Squares.c8)
            {
                isCastling = true;
                rookStartSquare = (int)BitboardUtil.Squares.a8;
                rookTargetSquare = (int)BitboardUtil.Squares.d8;
            }
        }


        // If EnPassant

        return new Move()
        {
            StartSquare = startSquare,
            TargetSquare = targetSquare,
            IsCapture = isCapture,
            IsPromotion = isPromotion,
            PromotionType = promotionType,
            IsCastling = isCastling,
            RookStartSquare = rookStartSquare,
            RookTargetSquare = rookTargetSquare,
            IsEnPassant = isEnPassant,
            TargetPawnSquare = targetPawnSquare,
            HasEnPassant = hasEnPassantTarget,
            EnPassantTargetSquare = enPassantTargetSquare,
        };
    }
}
