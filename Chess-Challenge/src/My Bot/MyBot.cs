using ChessChallenge.API;
using System;

public class MyBot : IChessBot
{
    Board board;
    Timer timer;
    Move last_best_move;

    ulong[] mg_eg_eval = {0, 18091310594175943473, 17729576757067842557, 17656368983588407033, 17583463653149966323, 18018903217143742195, 17659472831562776815, 0, 14698007690075952301, 17871161878745771228, 1595470717666860777, 795204270349158652, 18161338403160326906, 17873669855958727925, 17868322896468436722, 17723902009547159244, 18159617559699587826, 16791984640099813619, 18379780590394479352, 18375550773540487934, 145529233262118653, 362835568813868800, 4514607629076226, 17793153658603044848, 1517436131058128144, 1588962699736059917, 584929225523137022, 17797118570917002228, 17655233157860619246, 17365317221659243754, 15995946953292052714, 17576989728702528247, 1591202357256782066, 1949527540192177652, 2024117404048816378, 71784919331304435, 18375248330046108668, 146087707640791545, 71783832839650543, 16713409042533119744, 432890805913914336, 17504927689204957198, 17657211123293818108, 17220063970245408504, 16713116507612315880, 17580348611667294713, 289629968034169600, 508046960822194937, 0, 6724510076046890585, 3037988131755930159, 578715846390975504, 559642794984454, 18158792973527876354, 18230852792426431494, 0, 14979522056100179427, 16642195890504006900, 17075393589196224244, 17799357172263092728, 17798797507976297975, 17723633780620591093, 16930709953524266731, 16206194419948709874, 17652136907279955705, 18014111536846733052, 144119581827464193, 72344596705051903, 18230295327213486589, 18013555209917497338, 17580359705954220025, 17942051730983615733, 145247710940300550, 73185688757339653, 18446461507844899587, 72057598333288706, 18157667065048334849, 17941490980087529726, 18445614858104798717, 17727007052112396796, 721993266920885244, 4235443680840440, 290782441826943990, 1304940102892129025, 798000315483361015, 145531376283153400, 17361084063237142005, 17075100014892937968, 17870855050137956315, 363405120148801786, 438562246038849541, 75734416974679036, 18087594154091282167, 18159366965993799671, 17941780194679520243, 17002489315235131366};
    int[,,,] pestos_eval = new int[2,6,2,64];

    struct TTable {
        public ulong hash;
        public Move best_move;
        public int score, depth, bound;
        public TTable(ulong _hash, Move _best_move, int _score, int _depth, int _bound) {
            hash = _hash;
            best_move = _best_move;
            score = _score;
            depth = _depth;
            bound = _bound;
        }
    }

    const int entries = 2 << 20;
    TTable[] table = new TTable[entries];

    int[] game_phase_inc = {0,1,1,2,4,0};
    int val() {
        int game_phase = 0;
        int[] mg = {0, 0}, eg = {0, 0};

        for(int i=0; i<2; i++) {
            for(var pc = PieceType.Pawn; pc <= PieceType.King; pc++) {
                ulong mask = board.GetPieceBitboard(pc, i == 0);
                int p = (int)pc-1;
                while(mask != 0) {
                    game_phase += game_phase_inc[p];
                    int index = BitboardHelper.ClearAndGetIndexOfLSB(ref mask);
                    mg[i] += pestos_eval[0, p, i, index];
                    eg[i] += pestos_eval[1, p, i, index];
                }
            }
        }

        game_phase = Math.Max(game_phase, 24);
        int eg_phase = 24 - game_phase;
        
        return ((mg[0] - mg[1]) * game_phase + (eg[0] - eg[1]) * eg_phase) / 24;
    }

    int quiescence(int alpha, int beta) {
        int stand_pat = val();
        if(!board.IsWhiteToMove) stand_pat = -stand_pat;

        if(stand_pat >= beta) return beta;
        if(alpha < stand_pat) alpha = stand_pat;

        Move[] moves = board.GetLegalMoves(true);

        foreach(var move in moves) {
            board.MakeMove(move);
            int eval = -quiescence(-beta,-alpha);
            board.UndoMove(move);

            if(eval >= beta) return beta;
            if(eval > alpha) alpha = eval;
        }

        return alpha;
    }


    Tuple<int, int>[] sort_moves(Move[] moves, TTable entry) {
        Tuple<int, int>[] scores = new Tuple<int, int>[moves.Length];

        for(int i=0; i<moves.Length; i++) {

            if(entry.best_move == moves[i]) scores[i] = (10000000, i).ToTuple();
            else if(moves[i].IsCapture) scores[i] = (7 + (int)(moves[i].CapturePieceType - moves[i].MovePieceType), i).ToTuple();
            else scores[i] = (0, i).ToTuple();
        }
        
        Array.Sort(scores, (x, y) => y.Item1.CompareTo(x.Item1));

        return scores;
    }

    int search(int depth, int alpha, int beta, bool root) {
        if(board.IsInCheckmate()) return -100000001;
        else if(board.IsDraw()) return 0;

        Move best_move = Move.NullMove;

        ulong hash = board.ZobristKey;
        TTable entry = table[hash % entries];

        if(entry.hash == hash && 
           !root && 
           entry.depth >= depth &&
           (entry.bound == 3 ||
            entry.bound == 2 && entry.score >= beta ||
            entry.bound == 1 && entry.score <= alpha)) 
        {
            return entry.score; 
        }

        if(depth == 0) return quiescence(alpha, beta); 

        Move[] moves = board.GetLegalMoves();

        Tuple<int, int>[] sorted = sort_moves(moves, entry);

        int max_eval = -100000000, _alpha = alpha, eval, bound;
        foreach(var index in sorted) {
            if(timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30) return 10000000;
            Move move = moves[index.Item2];
            board.MakeMove(move);
            eval = -search(depth - 1, -beta, -alpha, false);
            board.UndoMove(move);

            if(eval > max_eval) {
                max_eval = eval;
                best_move = move;
                if(root) 
                    last_best_move = move;
                if(eval > alpha)
                    alpha = eval;

                if (alpha >= beta) break;

            }
        }

        bound = max_eval>= beta ? 2 : max_eval > _alpha ? 3 : 1;
        table[hash % entries] = new TTable(hash, best_move, max_eval, depth, bound);

        return max_eval;
    }

    public Move Think(Board b, Timer t)
    {
        board = b;
        timer = t;

        last_best_move = Move.NullMove;

        int[,] mg_eg_value = {{82, 337, 365, 477, 1025, 0}, {94, 281, 297, 512, 936, 0}};
        // int[,] mg_eg_value = {{100, 310, 330, 500, 1000, 10000 }, {100, 310, 330, 500, 1000, 10000 }};

        int i=0;
        for(; i<2; i++) 
            for(int j=0; j<6; j++)
                for(int k=0; k<8; k++)
                    for(int x=0; x<8; x++) {
                        int val = (int)(sbyte)(mg_eg_eval[i*6*8 + j*8 + k] >> x*8) * 2 + mg_eg_value[i, j];
                        pestos_eval[i, j, 0, k*8+x] = val;
                        pestos_eval[i, j, 1, (k*8+x)^56] = val;
                    }

        // ulong mask = board.GetPieceBitboard((PieceType)6, true);
        // int index = BitboardHelper.ClearAndGetIndexOfLSB(ref mask);
        // Console.WriteLine(index + " " + pestos_eval[0, 5, 1, index]);

        for(i=1; i<100; i++) {
            search(i, -100000000, 100000000, true);

            if(timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30)
                break;
        }   


        return last_best_move == Move.NullMove? board.GetLegalMoves()[0] : last_best_move;
    }
}