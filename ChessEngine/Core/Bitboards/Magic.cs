namespace ChessEngine.Core;

public static class Magic
{
    // Hash table for the magic values at each square index
    private static ulong[] _rookMasks = new ulong[64];
    private static ulong[] _bishopMasks = new ulong[64];

    // Hash table for moves available at each square with given magic blocker hash
    private static ulong[][] _rookMoves = new ulong[64][];
    private static ulong[][] _bishopMoves = new ulong[64][];

    // Precalculated shifts and magic keys from 
    // https://github.com/SebLague/Chess-Coding-Adventure/blob/Chess-V2-UCI/Chess-Coding-Adventure/src/Core/Move%20Generation/Magics/PrecomputedMagics.cs
    private static readonly int[] _rookShifts = { 52, 52, 52, 52, 52, 52, 52, 52, 53, 53, 53, 54, 53, 53, 54, 53, 53, 54, 54, 54, 53, 53, 54, 53, 53, 54, 53, 53, 54, 54, 54, 53, 52, 54, 53, 53, 53, 53, 54, 53, 52, 53, 54, 54, 53, 53, 54, 53, 53, 54, 54, 54, 53, 53, 54, 53, 52, 53, 53, 53, 53, 53, 53, 52 };
    private static readonly int[] _bishopShifts = { 58, 60, 59, 59, 59, 59, 60, 58, 60, 59, 59, 59, 59, 59, 59, 60, 59, 59, 57, 57, 57, 57, 59, 59, 59, 59, 57, 55, 55, 57, 59, 59, 59, 59, 57, 55, 55, 57, 59, 59, 59, 59, 57, 57, 57, 57, 59, 59, 60, 60, 59, 59, 59, 59, 60, 60, 58, 60, 59, 59, 59, 59, 59, 58 };

    private static readonly ulong[] _rookMagics = { 468374916371625120, 18428729537625841661, 2531023729696186408, 6093370314119450896, 13830552789156493815, 16134110446239088507, 12677615322350354425, 5404321144167858432, 2111097758984580, 18428720740584907710, 17293734603602787839, 4938760079889530922, 7699325603589095390, 9078693890218258431, 578149610753690728, 9496543503900033792, 1155209038552629657, 9224076274589515780, 1835781998207181184, 509120063316431138, 16634043024132535807, 18446673631917146111, 9623686630121410312, 4648737361302392899, 738591182849868645, 1732936432546219272, 2400543327507449856, 5188164365601475096, 10414575345181196316, 1162492212166789136, 9396848738060210946, 622413200109881612, 7998357718131801918, 7719627227008073923, 16181433497662382080, 18441958655457754079, 1267153596645440, 18446726464209379263, 1214021438038606600, 4650128814733526084, 9656144899867951104, 18444421868610287615, 3695311799139303489, 10597006226145476632, 18436046904206950398, 18446726472933277663, 3458977943764860944, 39125045590687766, 9227453435446560384, 6476955465732358656, 1270314852531077632, 2882448553461416064, 11547238928203796481, 1856618300822323264, 2573991788166144, 4936544992551831040, 13690941749405253631, 15852669863439351807, 18302628748190527413, 12682135449552027479, 13830554446930287982, 18302628782487371519, 7924083509981736956, 4734295326018586370 };
    private static readonly ulong[] _bishopMagics = { 16509839532542417919, 14391803910955204223, 1848771770702627364, 347925068195328958, 5189277761285652493, 3750937732777063343, 18429848470517967340, 17870072066711748607, 16715520087474960373, 2459353627279607168, 7061705824611107232, 8089129053103260512, 7414579821471224013, 9520647030890121554, 17142940634164625405, 9187037984654475102, 4933695867036173873, 3035992416931960321, 15052160563071165696, 5876081268917084809, 1153484746652717320, 6365855841584713735, 2463646859659644933, 1453259901463176960, 9808859429721908488, 2829141021535244552, 576619101540319252, 5804014844877275314, 4774660099383771136, 328785038479458864, 2360590652863023124, 569550314443282, 17563974527758635567, 11698101887533589556, 5764964460729992192, 6953579832080335136, 1318441160687747328, 8090717009753444376, 16751172641200572929, 5558033503209157252, 17100156536247493656, 7899286223048400564, 4845135427956654145, 2368485888099072, 2399033289953272320, 6976678428284034058, 3134241565013966284, 8661609558376259840, 17275805361393991679, 15391050065516657151, 11529206229534274423, 9876416274250600448, 16432792402597134585, 11975705497012863580, 11457135419348969979, 9763749252098620046, 16960553411078512574, 15563877356819111679, 14994736884583272463, 9441297368950544394, 14537646123432199168, 9888547162215157388, 18140215579194907366, 18374682062228545019 };

    public static void Initialise()
    {
        _rookMasks = new ulong[64];
        _bishopMasks = new ulong[64];

        _rookMoves = new ulong[64][];
        _bishopMoves = new ulong[64][];

        for (int i = 0; i < 64; i++)
        {
            _rookMasks[i] = GenerateRookMask(i);
            _rookMoves[i] = CreateHashTable(i, true);

            _bishopMasks[i] = GenerateBishopMask(i);
            _bishopMoves[i] = CreateHashTable(i, false);
        }
    }

    public static ulong RookMoves(int squareIndex, ulong blockers)
    {
        ulong mask = _rookMasks[squareIndex];
        ulong magic = _rookMagics[squareIndex];
        int shift = _rookShifts[squareIndex];
        ulong[] moves = _rookMoves[squareIndex];

        return moves[GetIndexHash(mask, blockers, magic, shift)];
    }

    public static ulong BishopMoves(int squareIndex, ulong blockers)
    {
        ulong mask = _bishopMasks[squareIndex];
        ulong magic = _bishopMagics[squareIndex];
        int shift = _bishopShifts[squareIndex];
        ulong[] moves = _bishopMoves[squareIndex];

        return moves[GetIndexHash(mask, blockers, magic, shift)];
    }

    private static ulong GetIndexHash(ulong mask, ulong blockers, ulong key, int shift)
    {
        blockers = blockers & mask;
        ulong index = (blockers * key) >> shift;
        return index;
    }

    private static ulong[] CreateHashTable(int square, bool isRook)
    {
        ulong mask;
        int shift;
        ulong magic;
        if (isRook)
        {
            mask = _rookMasks[square];
            shift = _rookShifts[square];
            magic = _rookMagics[square];
        }
        else
        {
            mask = _bishopMasks[square];
            shift = _bishopShifts[square];
            magic = _bishopMagics[square];
        }

        ulong[] table = new ulong[1 << 64 - shift];

        // Iterate through all subsets of blockers using Carry-Rippler method
        ulong subset = 0;
        do
        {
            ulong moves;
            if (isRook)
                moves = GenerateRookAttacks(square, subset);
            else
                moves = GenerateBishopAttacks(square, subset);

            ulong hash = GetIndexHash(mask, subset, magic, shift);
            table[hash] = moves;

            subset = (subset - mask) & mask;
        } while (subset != 0);
        return table;
    }

    private static ulong GenerateRookMask(int square)
    {
        ulong rookAttacks = 0;

        int rank, file;

        int targetRank = square / 8;
        int targetFile = square % 8;

        for (rank = targetRank, file = targetFile + 1; file < 7; file++)
            rookAttacks = BitboardUtil.AddBit(rookAttacks, rank * 8 + file);

        for (rank = targetRank, file = targetFile - 1; file > 0; file--)
            rookAttacks = BitboardUtil.AddBit(rookAttacks, rank * 8 + file);

        for (rank = targetRank + 1, file = targetFile; rank < 7; rank++) 
            rookAttacks = BitboardUtil.AddBit(rookAttacks, rank * 8 + file);

        for (rank = targetRank - 1, file = targetFile; rank > 0; rank--)
            rookAttacks = BitboardUtil.AddBit(rookAttacks, rank * 8 + file);

        return rookAttacks;
    }

    private static ulong GenerateRookAttacks(int square, ulong blockers)
    {
        ulong rookAttacks = 0;

        int rank, file;

        int targetRank = square / 8;
        int targetFile = square % 8;

        // Mask rook attacks with checking for pieces stopping the attack
        for (rank = targetRank, file = targetFile + 1;
             file <= 7;
             file++)
        {
            int squareIndex = rank * 8 + file;

            rookAttacks = BitboardUtil.AddBit(rookAttacks, squareIndex);

            if (BitboardUtil.GetBit(blockers, squareIndex) != 0)
                break;
        }

        for (rank = targetRank, file = targetFile - 1;
             file >= 0;
             file--)
        {
            int squareIndex = rank * 8 + file;

            rookAttacks = BitboardUtil.AddBit(rookAttacks, squareIndex);

            if (BitboardUtil.GetBit(blockers, squareIndex) != 0)
                break;
        }

        for (rank = targetRank + 1, file = targetFile;
             rank <= 7;
             rank++)
        {
            int squareIndex = rank * 8 + file;

            rookAttacks = BitboardUtil.AddBit(rookAttacks, squareIndex);

            if (BitboardUtil.GetBit(blockers, squareIndex) != 0)
                break;
        }

        for (rank = targetRank - 1, file = targetFile;
             rank >= 0;
             rank--)
        {
            int squareIndex = rank * 8 + file;

            rookAttacks = BitboardUtil.AddBit(rookAttacks, squareIndex);

            if (BitboardUtil.GetBit(blockers, squareIndex) != 0)
                break;
        }

        return rookAttacks;
    }

    private static ulong GenerateBishopMask(int square)
    {
        ulong bishopAttacks = 0;

        int rank, file;

        int targetRank = square / 8;
        int targetFile = square % 8;

        for (rank = targetRank + 1, file = targetFile + 1; rank < 7 && file < 7; rank++, file++)
            bishopAttacks = BitboardUtil.AddBit(bishopAttacks, rank * 8 + file);

        for (rank = targetRank - 1, file = targetFile + 1; rank > 0 && file < 7;  rank--, file++)
            bishopAttacks = BitboardUtil.AddBit(bishopAttacks, rank * 8 + file);

        for (rank = targetRank + 1, file = targetFile - 1; rank < 7 && file > 0; rank++, file--)
            bishopAttacks = BitboardUtil.AddBit(bishopAttacks, rank * 8 + file);

        for (rank = targetRank - 1, file = targetFile - 1; rank > 0 && file > 0; rank--, file--)
            bishopAttacks = BitboardUtil.AddBit(bishopAttacks, rank * 8 + file);

        return bishopAttacks;
    }

    private static ulong GenerateBishopAttacks(int square, ulong blockers)
    {
        ulong bishopAttacks = 0;

        int rank, file;

        int targetRank = square / 8;
        int targetFile = square % 8;

        // Mask squares the bishop can move to from it's position checking for
        // pieces that would block an attack.
        for (rank = targetRank + 1, file = targetFile + 1;
             rank <= 7 && file <= 7;
             rank++, file++)
        {
            int squareIndex = rank * 8 + file;

            bishopAttacks = BitboardUtil.AddBit(bishopAttacks, squareIndex);

            // Break loop if blocker is on this square as bishop can't attack
            // anything after it.
            if (BitboardUtil.GetBit(blockers, squareIndex) != 0)
                break;
        }

        for (rank = targetRank - 1, file = targetFile + 1;
             rank >= 0 && file <= 7;
             rank--, file++)
        {
            int squareIndex = rank * 8 + file;

            bishopAttacks = BitboardUtil.AddBit(bishopAttacks, squareIndex);

            if (BitboardUtil.GetBit(blockers, squareIndex) != 0)
                break;
        }

        for (rank = targetRank + 1, file = targetFile - 1;
             rank <= 7 && file >= 0;
             rank++, file--)
        {
            int squareIndex = rank * 8 + file;

            bishopAttacks = BitboardUtil.AddBit(bishopAttacks, squareIndex);

            if (BitboardUtil.GetBit(blockers, squareIndex) != 0)
                break;
        }

        for (rank = targetRank - 1, file = targetFile - 1;
             rank >= 0 && file >= 0;
             rank--, file--)
        {
            int squareIndex = rank * 8 + file;

            bishopAttacks = BitboardUtil.AddBit(bishopAttacks, squareIndex);

            if (BitboardUtil.GetBit(blockers, squareIndex) != 0)
                break;
        }

        return bishopAttacks;
    }
}
