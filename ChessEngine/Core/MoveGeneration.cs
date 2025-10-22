namespace ChessEngine.Core;

public static class MoveGeneration
{
    /// <summary>
    /// Generates all legal moves from a given position (state of the board)
    /// </summary>
    /// <param name="position">The board state</param>
    /// <returns>Legal Moves list</returns>
    public static List<Move> GenerateMoves(Board position)
    {
        List<Move> moves = [];

        int kingSquare = BitboardUtil.GetLeastSignificantBit(
            position.PieceBitboards[position.ColourToMove | Piece.King]);

        /* Generate opposition attacks without friendly king to ensure moves that
         * leave the king in check (moving along a slider piece's attack) are not
         * created.
         */
        ulong oppositionAttacks = AttackBitboards.GetAllAttacks(
            position.OppositionColour,
            BitboardUtil.RemoveBit(position.OccupiedBitboard, kingSquare),
            position.PieceBitboards);
      
        moves.AddRange(KingMoves(position, kingSquare, oppositionAttacks));

        ulong checkers = GetCheckers(position, kingSquare);
        int checkerCount = BitboardUtil.Count(checkers);
        position.IsCheck = checkerCount > 0; 

        // Double check, only king moves are possible to get out of check
        if (checkerCount > 1)
            return moves;

        /* Calculate a move mask based on if/what piece is checking the king.
         * The mask is used to limit the moves of the other pieces.
         * If the piece is a slider then moves that block and capture the 
         * attacker are allowed. Else only capturing the attack piece will stop
         * the check. By default all squares are allowed (all on bitboard).
         */
        ulong moveMask = ulong.MaxValue;
        if (checkerCount == 1)
        {
            moveMask = checkers;

            int checkerSquare = BitboardUtil.GetLeastSignificantBit(checkers);
            if (Piece.IsSlider(position.BoardSquares[checkerSquare]))
            {
                moveMask |= AttackBitboards
                    .GetDiagonalAttackRay(checkerSquare, kingSquare);
                moveMask |= AttackBitboards
                    .GetOrthogonalAttackRay(checkerSquare, kingSquare);
            }
        }

        // Calculate absolute pins
        // Calculate a mask of squares that would pin pieces to the king.
        // Create an array of empty bitboards then populate with opponent's sliding
        // pieces.
        Dictionary<int, ulong> pinMasks = [];

        // Diagonal pins (bishops, queens)
        ulong opponentPieces = position.
            PieceBitboards[position.OppositionColour | Piece.Bishop];

        opponentPieces |= position.
            PieceBitboards[position.OppositionColour | Piece.Queen];

        int[] activeBits = BitboardUtil.GetActiveBits(opponentPieces);
        for (int i = 0; i < activeBits.Length; i++)
        {
            int square = activeBits[i];

            CalculatePinMasks(
                position,
                pinMasks,
                AttackBitboards.GetDiagonalAttackRay(square, kingSquare),
                square);
        }

        // Orthogonal pins (rooks, queens)
        opponentPieces = position.
            PieceBitboards[position.OppositionColour | Piece.Rook];

        opponentPieces |= position.
            PieceBitboards[position.OppositionColour | Piece.Queen];
        
        activeBits = BitboardUtil.GetActiveBits(opponentPieces);
        for (int i = 0; i < activeBits.Length; i++)
        {
            int square = activeBits[i];

            CalculatePinMasks(
                position,
                pinMasks,
                AttackBitboards.GetOrthogonalAttackRay(square, kingSquare),
                square);
        }

        moves.AddRange(CastlingMoves(position, oppositionAttacks));

        moves.AddRange(PawnMoves(position, moveMask, pinMasks));

        moves.AddRange(KnightMoves(position, moveMask, pinMasks));

        moves.AddRange(BishopMoves(position, moveMask, pinMasks));

        moves.AddRange(RookMoves(position, moveMask, pinMasks));

        moves.AddRange(QueenMoves(position, moveMask, pinMasks));

        return moves;
    }

    private static List<Move> KingMoves(
        Board position, 
        int kingSquare, 
        ulong oppositionAttacks)
    {
        ulong kingAttacks = AttackBitboards.KingAttacks[kingSquare];

        // Remove king attacks on squares attacked by the opposition
        ulong illegalKingAttacks = kingAttacks & oppositionAttacks;

        // Remove king attacks blocked by friendly pieces
        ulong blockedKingAttacks = kingAttacks & 
            position.PieceBitboards[position.ColourToMove];

        kingAttacks ^= illegalKingAttacks | blockedKingAttacks;

        return CreateMoves(position, kingSquare, kingAttacks);
    }

    private static List<Move> CastlingMoves(
        Board position, 
        ulong oppositionAttacks)
    {
        List<Move> moves = [];

        if (position.IsCheck)
            return moves;

        ulong castleAttacked;
        ulong castleBlocked;
        if (position.ColourToMove == Piece.White)
        {
            castleAttacked = oppositionAttacks &
                BitboardUtil.WhiteKingSideCastleMask;

            castleBlocked = position.OccupiedBitboard &
                BitboardUtil.WhiteKingSideCastleMask;

            if (position.CanWhiteKingSideCastle && castleAttacked.Equals(0) && 
                castleBlocked.Equals(0))
            {
                moves.Add(new Move()
                {
                    StartSquare = (int)BitboardUtil.Squares.e1,
                    TargetSquare = (int)BitboardUtil.Squares.g1,
                    IsCastling = true,
                    RookStartSquare = (int)BitboardUtil.Squares.h1,
                    RookTargetSquare = (int)BitboardUtil.Squares.f1,
                });
            }

            castleAttacked = oppositionAttacks &
                BitboardUtil.WhiteQueenSideCastleMask;

            castleBlocked = position.OccupiedBitboard &
                BitboardUtil.WhiteQueenSideCastleBlockMask;

            if (position.CanWhiteQueenSideCastle && castleAttacked.Equals(0) && 
                castleBlocked.Equals(0))
            {
                moves.Add(new Move()
                {
                    StartSquare = (int)BitboardUtil.Squares.e1,
                    TargetSquare = (int)BitboardUtil.Squares.c1,
                    IsCastling = true,
                    RookStartSquare = (int)BitboardUtil.Squares.a1,
                    RookTargetSquare = (int)BitboardUtil.Squares.d1,
                });
            }
        }
        else
        {
            castleAttacked = oppositionAttacks &
                BitboardUtil.BlackKingSideCastleMask;

            castleBlocked = position.OccupiedBitboard &
                BitboardUtil.BlackKingSideCastleMask;

            if (position.CanBlackKingSideCastle && castleAttacked.Equals(0) &&
                castleBlocked.Equals(0))
            {
                moves.Add(new Move()
                {
                    StartSquare = (int)BitboardUtil.Squares.e8,
                    TargetSquare = (int)BitboardUtil.Squares.g8,
                    IsCastling = true,
                    RookStartSquare = (int)BitboardUtil.Squares.h8,
                    RookTargetSquare = (int)BitboardUtil.Squares.f8,
                });
            }

            castleAttacked = oppositionAttacks &
                BitboardUtil.BlackQueenSideCastleMask;

            castleBlocked = position.OccupiedBitboard &
                BitboardUtil.BlackQueenSideCastleBlockMask;

            if (position.CanBlackQueenSideCastle && castleAttacked.Equals(0) &&
                castleBlocked.Equals(0))
            {
                moves.Add(new Move()
                {
                    StartSquare = (int)BitboardUtil.Squares.e8,
                    TargetSquare = (int)BitboardUtil.Squares.c8,
                    IsCastling = true,
                    RookStartSquare = (int)BitboardUtil.Squares.a8,
                    RookTargetSquare = (int)BitboardUtil.Squares.d8,
                });
            }
        }

        return moves;
    }

    private static List<Move> PawnMoves(
        Board position, 
        ulong moveMask,
        Dictionary<int, ulong> pinMasks)
    {
        List<Move> moves = [];
        ulong pawns = position.PieceBitboards[Piece.Pawn | position.ColourToMove];

        int pawnCount = BitboardUtil.Count(pawns);
        for (int i = 0; i < pawnCount; i++)
        {
            int pawnSquare = BitboardUtil.GetLeastSignificantBit(pawns);
            ulong pawn = BitboardUtil.AddBit(0, pawnSquare);

            int pawnPushOffset = position.ColourToMove == Piece.White ? -8 : 8;
            ulong promotionRank = position.ColourToMove == Piece.White ?
                BitboardUtil.Rank8Mask : BitboardUtil.Rank1Mask;
            ulong startingRank = position.ColourToMove == Piece.White ?
                BitboardUtil.Rank2Mask : BitboardUtil.Rank7Mask;

            ulong oppPieces = position.PieceBitboards[position.OppositionColour];
            ulong pawnAttacks = AttackBitboards.PawnAttacks[position.ColourToMove, pawnSquare];

            ulong combinedMoveMask;
            if (pinMasks.TryGetValue(pawnSquare, out ulong pinMask))
            {
                combinedMoveMask = pinMask & moveMask;
            }
            else
            {
                combinedMoveMask = moveMask;
            }

            // Pawn moves single and double
            // Single move is separate so double move can extended regardless of legality
            ulong singleMove = BitboardUtil.AddBit(0, pawnSquare + pawnPushOffset) 
                & position.EmptyBitboard;
            ulong legalSingleMove = singleMove & combinedMoveMask;
            if ((legalSingleMove & promotionRank) == 0)
            {
                moves.AddRange(CreateMoves(position, pawnSquare, legalSingleMove));
            }
            else
            {
                moves.AddRange(CreatePromotionMoves(position, pawnSquare, legalSingleMove));
            }

            // If the pawn is on it's starting rank and has a legal double move 
            ulong legalDoubleMove = BitboardUtil.AddBit(0, pawnSquare + (pawnPushOffset * 2)) 
                & position.EmptyBitboard & combinedMoveMask;
            if ((pawn & startingRank) != 0 && singleMove != 0 && legalDoubleMove != 0)
            {
                // Check if double move creates en passant opportunity
                int targetSquare = BitboardUtil.GetLeastSignificantBit(legalDoubleMove);
                int enPassantSquare = targetSquare - pawnPushOffset;
                ulong attackedBySquares = AttackBitboards
                    .PawnAttacks[position.ColourToMove, enPassantSquare];
                ulong oppPawns = position
                    .PieceBitboards[position.OppositionColour | Piece.Pawn];

                Move move = new()
                {
                    StartSquare = pawnSquare,
                    TargetSquare = targetSquare
                };

                if ((oppPawns & attackedBySquares) != 0)
                {
                    move.HasEnPassant = true;
                    move.EnPassantTargetSquare = enPassantSquare;
                }
                moves.Add(move);
            }

            // Pawn captures
            ulong legalAttacks = pawnAttacks & oppPieces & combinedMoveMask;
            if ((legalAttacks & promotionRank) == 0)
            {
                moves.AddRange(CreateMoves(position, pawnSquare, legalAttacks));
            }
            else
            {
                moves.AddRange(CreatePromotionMoves(position, pawnSquare, legalAttacks));
            }

            // EnPassant
            if (position.HasEnPassantTargetSquare)
            {
                // Check if en passant square is attackable
                int enPassantSquare = position.EnPassantTargetSquare;
                ulong enPassant = BitboardUtil.AddBit(0, enPassantSquare);
                ulong enPassantAttacks = (enPassant & pawnAttacks);

                if (enPassantAttacks != 0)
                {
                    // Create separate move mask to allow en passant if opp pawn is checking
                    int oppPawnSquare = enPassantSquare - pawnPushOffset;
                    ulong enPassantMoveMask = combinedMoveMask;

                    if (BitboardUtil.GetBit(enPassantMoveMask, oppPawnSquare) != 0)
                    {
                        enPassantMoveMask = BitboardUtil
                            .AddBit(enPassantMoveMask, enPassantSquare);
                    } 

                    if ((enPassantAttacks & enPassantMoveMask) != 0)
                    {
                        // Check if en passant would cause illegal check from orthogonal attacker
                        // Include where pawn will be after capture to block vertical attacks
                        ulong blockingPawns = BitboardUtil
                            .AddBit(pawn, oppPawnSquare);
                        blockingPawns = BitboardUtil
                            .AddBit(blockingPawns, enPassantSquare);

                        if (!WouldEnPassantCauseCheck(position, blockingPawns))
                        {
                            moves.Add(new Move()
                            {
                                StartSquare = pawnSquare,
                                TargetSquare = enPassantSquare,
                                TargetPawnSquare = oppPawnSquare,
                                IsCapture = true,
                                IsEnPassant = true,
                                CapturedPiece = position.OppositionColour | Piece.Pawn
                            });
                        }
                    }
                }
            }

            // Removed the moved pawn from bitboard
            pawns ^= pawn;
        }
        return moves;
    }

    private static List<Move> KnightMoves(
        Board position, 
        ulong moveMask,
        Dictionary<int, ulong> pinMasks)
    {
        List<Move> moves = [];

        ulong knights = 
            position.PieceBitboards[Piece.Knight | position.ColourToMove];

        int[] activeBits = BitboardUtil.GetActiveBits(knights);
        for (int i = 0; i < activeBits.Length; i++)
        { 
            int startSquare = activeBits[i];

            // L shape move pattern means knight can never move along or capture
            // when pinned
            if (pinMasks.ContainsKey(startSquare))
                continue;

            // Get attacks for a knight on that square
            ulong attacks = AttackBitboards.KnightAttacks[startSquare];

            // AND the friendly pieces to the bitboard to give blocked attacks
            ulong blocked = attacks & position.PieceBitboards[position.ColourToMove];

            // XOR the blocked attacks with normal attacks giving unblocked attacks
            // or captures
            attacks ^= blocked;
            attacks &= moveMask;

            moves.AddRange(CreateMoves(position, startSquare, attacks));
        }

        return moves;
    }

    private static List<Move> BishopMoves(
        Board position, 
        ulong moveMask,
        Dictionary<int, ulong> pinMasks)
    {
        List<Move> moves = [];

        ulong bishops = 
            position.PieceBitboards[Piece.Bishop | position.ColourToMove];

        int[] activeBits = BitboardUtil.GetActiveBits(bishops);
        for (int i = 0; i < activeBits.Length; i++)
        {
            int startSquare = activeBits[i];

            ulong combinedMoveMask;
            // If pinned piece alter move mask with pin
            if (pinMasks.TryGetValue(startSquare, out ulong pinMask))
                combinedMoveMask = pinMask & moveMask;
            else
                combinedMoveMask = moveMask;

            // Calculate attacks for a bishop on that square given other pieces on the board 
            ulong attacks = 
                AttackBitboards.GetBishopAttacks(
                    startSquare, position.OccupiedBitboard);

            // AND friendly pieces to get blocked attacks
            ulong blocked = attacks & position.PieceBitboards[position.ColourToMove];

            // XOR blocked attacks to give only unblocked attacks and captures
            attacks ^= blocked;
            attacks &= combinedMoveMask;

            moves.AddRange(CreateMoves(position, startSquare, attacks));
        }

        return moves;
    }

    private static List<Move> RookMoves(
        Board position, 
        ulong moveMask, 
        Dictionary<int, ulong> pinMasks) 
    {
        List<Move> moves = [];

        ulong rooks = 
            position.PieceBitboards[Piece.Rook | position.ColourToMove];

        int[] activeBits = BitboardUtil.GetActiveBits(rooks);
        for (int i = 0; i < activeBits.Length; i++)
        {
            int startSquare = activeBits[i];

            ulong combinedMoveMask;
            // If pinned piece alter move mask with pin
            if (pinMasks.TryGetValue(startSquare, out ulong pinMask))
                combinedMoveMask = pinMask & moveMask;
            else
                combinedMoveMask = moveMask;

            ulong attacks = AttackBitboards
                .GetRookAttacks(startSquare, position.OccupiedBitboard);

            ulong blocked = attacks & position.PieceBitboards[position.ColourToMove];

            attacks ^= blocked;
            attacks &= combinedMoveMask;

            moves.AddRange(CreateMoves(position, startSquare, attacks));
        }

        return moves;
    }

    private static List<Move> QueenMoves(
        Board position, 
        ulong moveMask,
        Dictionary<int, ulong> pinMasks) 
    {
        List<Move> moves = [];

        ulong queens = 
            position.PieceBitboards[Piece.Queen | position.ColourToMove];

        int[] activeBits = BitboardUtil.GetActiveBits(queens);
        for (int i = 0; i < activeBits.Length; i++)
        {
            int startSquare = activeBits[i];

            ulong combinedMoveMask;
            // If pinned piece alter move mask with pin
            if (pinMasks.TryGetValue(startSquare, out ulong pinMask))
                combinedMoveMask = pinMask & moveMask;
            else
                combinedMoveMask = moveMask;

            ulong attacks = AttackBitboards
                .GetQueenAttacks(startSquare, position.OccupiedBitboard);
            
            ulong blocked = attacks & position.PieceBitboards[position.ColourToMove];

            attacks ^= blocked;
            attacks &= combinedMoveMask;

            moves.AddRange(CreateMoves(position, startSquare, attacks));
        }

        return moves;
    }

    /// <summary>
    /// Calculates if any opponent pieces are on squares that would attack the
    /// king (putting him in check).
    /// </summary>
    /// <param name="position">The current board state</param>
    /// <param name="kingSquare">The square's index the king occupies</param>
    /// <returns>A bitboard representation of the checkers. Returns as an empty 
    /// bitboard if non.</returns>
    private static ulong GetCheckers(Board position, int kingSquare)
    {
        ulong checkers = 0;

        checkers |= position.PieceBitboards[position.OppositionColour | Piece.Pawn] & 
            AttackBitboards.PawnAttacks[position.ColourToMove, kingSquare];

        checkers |= position.PieceBitboards[position.OppositionColour | Piece.Knight] &
            AttackBitboards.KnightAttacks[kingSquare];

        checkers |= position.PieceBitboards[position.OppositionColour | Piece.Bishop] &
            AttackBitboards.GetBishopAttacks(kingSquare, position.OccupiedBitboard);

        checkers |= position.PieceBitboards[position.OppositionColour | Piece.Rook] &
            AttackBitboards.GetRookAttacks(kingSquare, position.OccupiedBitboard);

        checkers |= position.PieceBitboards[position.OppositionColour | Piece.Queen] &
            AttackBitboards.GetQueenAttacks(kingSquare, position.OccupiedBitboard);

        return checkers;
    }

    private static void CalculatePinMasks(
        Board position,
        Dictionary<int, ulong> pinMasks,
        ulong attackRay,
        int square)
    {
        ulong pinnedPieces = attackRay & position.PieceBitboards[position.ColourToMove];

        // Ignore rays blocked by opponent pieces
        if ((attackRay & position.PieceBitboards[position.OppositionColour]) != 0)
            return;

        if (attackRay == 0 || BitboardUtil.Count(pinnedPieces) != 1)
            return;

        int pinnedSquare = BitboardUtil.GetLeastSignificantBit(pinnedPieces);

        /* Remove square that pinned piece occupies and add square of the
         * pin attacker to give all moves along pin
         */
        ulong pinnedMoveMask = BitboardUtil.RemoveBit(attackRay, pinnedSquare);
        pinnedMoveMask = BitboardUtil.AddBit(pinnedMoveMask, square);

        pinMasks.Add(pinnedSquare, pinnedMoveMask);
    }

    /// <summary>
    /// Creates a list of individual moves from each of the moves within the 
    /// provided bitboard.
    /// </summary>
    /// <param name="position">The current board state</param>
    /// <param name="startSquare">The square the piece is moving from</param>
    /// <param name="bitboard">The bitboard containing the moves</param>
    /// <returns>List of individual moves</returns>
    private static List<Move> CreateMoves(
        Board position, 
        int startSquare, 
        ulong bitboard)
    {
        List<Move> moves = [];

        int[] attackSquares = BitboardUtil.GetActiveBits(bitboard);
        for (int i = 0; i < attackSquares.Length; i++)
        {
            int targetSquare = attackSquares[i];

            bool isCapture =
                BitboardUtil.GetBit(position.OccupiedBitboard, targetSquare) != 0;

            moves.Add(new()
            {
                StartSquare = startSquare,
                TargetSquare = targetSquare,
                IsCapture = isCapture,
                CapturedPiece = position.BoardSquares[targetSquare]
            });
        }

        return moves;
    }

    private static bool WouldEnPassantCauseCheck(
        Board position,
        ulong blockingPawns)
    {
        ulong blockers = position.OccupiedBitboard ^ blockingPawns;

        int kingSquare = BitboardUtil.GetLeastSignificantBit(
            position.PieceBitboards[position.ColourToMove | Piece.King]);
        
        ulong attacks = AttackBitboards.GetRookAttacks(kingSquare, blockers);
        // Attacks could be from opponents queens or rooks
        ulong oppSilders = 
            position.PieceBitboards[position.OppositionColour | Piece.Rook] |
            position.PieceBitboards[position.OppositionColour | Piece.Queen];
        return (attacks & oppSilders) != 0;
    }

    private static List<Move> CreatePromotionMoves(
        Board position,
        int startSquare,
        ulong bitboard)
    {
        List<Move> moves = [];

        int[] attackSqaures = BitboardUtil.GetActiveBits(bitboard);
        for (int i = 0; i < attackSqaures.Length; i++)
        {
            int targetSquare = attackSqaures[i];

            bool isCapture =
                BitboardUtil.GetBit(position.OccupiedBitboard, targetSquare) != 0;

            for (int j = 0; j < Piece.PromotionTypes.Length; j++)
            {
                moves.Add(new()
                {
                    StartSquare = startSquare,
                    TargetSquare = targetSquare,
                    IsCapture = isCapture,
                    CapturedPiece = position.BoardSquares[targetSquare],
                    IsPromotion = true,
                    PromotionType = Piece.PromotionTypes[j]
                });
            }
        }

        return moves;
    }
}
