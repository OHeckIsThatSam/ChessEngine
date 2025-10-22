namespace ChessEngine.Core;

public static class Evaluation
{
    // Material value for all piece types (in centi pawns)
    // Source https://www.chessprogramming.org/Simplified_Evaluation_Function
    private readonly static Dictionary<int, int> PieceValue = new()
    {
        { Piece.Pawn, 100 },
        { Piece.Knight, 320 },
        { Piece.Bishop, 330 },
        { Piece.Rook, 500 },
        { Piece.Queen, 900 },
        { Piece.King, 20000 }
    };

    public enum GamePhase
    {
        Middle,
        End
    }

    private static GamePhase _phase = GamePhase.Middle;

    /// <summary>
    /// Evaluates the position of the provided board. Factors that impact the score;  
    /// Balance of material, ...
    /// 
    /// Note:
    /// The score's sign + or - is subject to the colour to move. e.g. if the 
    /// position is evaluated to be 3 points better for white then if it's black's 
    /// move then the position is -3 or +3 if it's white to move. 
    /// This is essential to work with the NegaMax search algorithm.
    /// </summary>
    /// <param name="board">The current state of the board</param>
    /// <returns>The score of the position, subject to the colour to move</returns>
    public static int Evaluate(Board board)
    {
        // Calculate game phase
        // If neither queens are on the board or if each side with a queen has no other minor
        // pieces
        ulong whiteQueen = board.PieceBitboards[Piece.WhiteQueen];
        ulong blackQueen = board.PieceBitboards[Piece.BlackQueen];
        int whiteMinorCount = BitboardUtil.Count(board.PieceBitboards[Piece.WhiteBishop]
            | board.PieceBitboards[Piece.WhiteKnight]);
        int blackMinorCount = BitboardUtil.Count(board.PieceBitboards[Piece.BlackBishop]
            | board.PieceBitboards[Piece.BlackKnight]);
        if ((whiteQueen | blackQueen) == 0 )
        {
            _phase = GamePhase.End;
        }
        else if (whiteMinorCount <= 1 && board.PieceBitboards[Piece.WhiteRook] == 0 
            && blackMinorCount <= 1 && board.PieceBitboards[Piece.BlackRook] == 0)
        {
            _phase = GamePhase.End;
        }
        else
        {
            _phase = GamePhase.Middle;
        }


        int[] material = new int[2];

        for (int i = 0; i < Piece.AllPieceTypes.Length; i++)
        {
            int piece = Piece.AllPieceTypes[i];
            ulong bitboard = board.PieceBitboards[piece];

            int colourIndex = Piece.GetPieceColour(piece) == Piece.White ? 0 : 1;

            // Get inherent value
            int value = PieceValue[Piece.GetPieceType(piece)];

            while(bitboard != 0)
            {
                int square = BitboardUtil.PopLSB(ref bitboard);
                int positionalValue = piece switch
                {
                    Piece.Pawn => PieceTables.Pawns[colourIndex][square],
                    Piece.Knight => PieceTables.Knights[colourIndex][square],
                    Piece.Bishop => PieceTables.Bishops[colourIndex][square],
                    Piece.Rook => PieceTables.Rooks[colourIndex][square],
                    Piece.Queen => PieceTables.Queens[colourIndex][square],
                    Piece.King => _phase != GamePhase.End ? 
                        PieceTables.Kings[colourIndex][square] : PieceTables.KingsEndGame[colourIndex][square], 
                    _ => 0,
                };

                material[colourIndex] += value + positionalValue;
            }
        }

        int materialIndex = board.ColourToMove == Piece.White ? 0 : 1;

        return material[materialIndex] - material[materialIndex ^ 1];
    }
}
