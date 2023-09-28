using ChessChallenge.API;
using System;
using System.Diagnostics;

public class MyBot : IChessBot
{
    Board board;
    Move last_best_move;

    // ulong[] mg_eg_eval = {0, 18091310594175943473, 17729576757067842557, 17656368983588407033, 17583463653149966323, 18018903217143742195, 17659472831562776815, 0, 14698007690075952301, 17871161878745771228, 1595470717666860777, 795204270349158652, 18161338403160326906, 17873669855958727925, 17868322896468436722, 17723902009547159244, 18159617559699587826, 16791984640099813619, 18379780590394479352, 18375550773540487934, 145529233262118653, 362835568813868800, 4514607629076226, 17793153658603044848, 1517436131058128144, 1588962699736059917, 584929225523137022, 17797118570917002228, 17655233157860619246, 17365317221659243754, 15995946953292052714, 17576989728702528247, 1591202357256782066, 1949527540192177652, 2024117404048816378, 71784919331304435, 18375248330046108668, 146087707640791545, 71783832839650543, 16713409042533119744, 432890805913914336, 17504927689204957198, 17657211123293818108, 17220063970245408504, 16713116507612315880, 17580348611667294713, 289629968034169600, 508046960822194937, 0, 6724510076046890585, 3037988131755930159, 578715846390975504, 559642794984454, 18158792973527876354, 18230852792426431494, 0, 14979522056100179427, 16642195890504006900, 17075393589196224244, 17799357172263092728, 17798797507976297975, 17723633780620591093, 16930709953524266731, 16206194419948709874, 17652136907279955705, 18014111536846733052, 144119581827464193, 72344596705051903, 18230295327213486589, 18013555209917497338, 17580359705954220025, 17942051730983615733, 145247710940300550, 73185688757339653, 18446461507844899587, 72057598333288706, 18157667065048334849, 17941490980087529726, 18445614858104798717, 17727007052112396796, 721993266920885244, 4235443680840440, 290782441826943990, 1304940102892129025, 798000315483361015, 145531376283153400, 17361084063237142005, 17075100014892937968, 17870855050137956315, 363405120148801786, 438562246038849541, 75734416974679036, 18087594154091282167, 18159366965993799671, 17941780194679520243, 17002489315235131366};
    // int[,,,] pestos_eval = new int[2,6,2,64];
    ulong[] mg_eg_eval = {0, 0, 4841175116161440049, 6772096698761038114, 2382168964064292861, 3095706925818190880, 146936547999355129, 645430952084307723, 18232541607926630131, 68681019531984136, 70929486584873715, 18228882514716459009, 429254794159326959, 18299533842006673145, 0, 0, 17503515860169712557, 14973309849777008926, 18380021468951803100, 16643320720765484043, 297243154403226857, 17012056380661563434, 799964017111660796, 17801326388899351314, 866388813012989946, 17869159663607023630, 505810563213620725, 17724186869195343113, 18374682059997637618, 16931271778046181120, 18010165269052715724, 16210117485992801784, 18225781646978906610, 17653257335397416436, 18084771617313455347, 18008203903822069519, 5629593957564920, 215891384498519825, 295273886361255934, 143834825580742418, 652184143895395837, 18159357044535460625, 362262727416609280, 17944026505441773063, 278214946060546, 17510269202559795715, 18372142183944812016, 17939806463714852090, 511449997092652560, 150030625158727199, 369020416193201421, 78254497425653544, 149185092077028350, 18377217688567415304, 3665784685134580, 141301520641622284, 144120659913671150, 18155694566890405380, 70086135345643242, 17938115487862947073, 142990348991921642, 18437169517798226944, 2252899358538999, 17794569778090999304, 938449727295388914, 726773977771805981, 1441170564049664244, 7616377378315768, 1730512567815173882, 296121670175561486, 1655087122550555123, 1297068574561016832, 1727985860213995516, 864430548796182527, 287957679870179577, 144683665786471678, 17870829804286965231, 17294085352197715972, 16935212440553517056, 17070884431561293561, 17868584614311484384, 17871974374717783012, 647682674688850446, 428418027424188668, 574221051547616764, 501331170391427830, 1005161409915845880, 139064001877511665, 931412740990367720, 18151482252178623977, 786166164072560633, 18226916485030284266, 495399244577108736, 17871688514496104427, 18151184382827882233, 16935773260320272900};
    int[] pestos_eval = new int[2 * 96 * 8];

    public MyBot() {
        int[,] mg_eg_value = {{95, 337, 365, 477, 1025, 10000}, {105, 281, 297, 512, 936, 10000}};
        
        for(int i=0; i<mg_eg_eval.Length; i++)
            for(int j=0; j<4; j++)
                for(int g=0; g<2; g++) {
                    int val = (int)(sbyte)(mg_eg_eval[i] >> (j*2+g)*8) * 2 + mg_eg_value[g, i/16];

                    pestos_eval[(i*8 / 128)*128 + ((i*8+j*2) % 128)^112 + g] = val;

                    pestos_eval[(i*8+j*2+g)+768] = val;
                }

        // for(int i=0; i<6; i++) {
        //     for(int j=0; j<64; j++) {
        //         if(j % 8 == 0) Console.WriteLine();
        //         Console.Write(pestos_eval[i*128 + 2*j] + " ");
        //     }
        //     Console.WriteLine("\n");
        //     for(int j=0; j<64; j++) {
        //         if(j % 8 == 0) Console.WriteLine();
        //         Console.Write(pestos_eval[i*128 + 2*j+1] + " ");
        //     }
        //     Console.WriteLine("\n\n");
        // }
    }

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
    unsafe int val() {
        int mg = 0, eg = 0, game_phase = 0;

        for(int i=0; i<2; i++) {
            for(int p = 0; p <= 5; p++) {
                ulong mask = board.GetPieceBitboard((PieceType)p+1, i == 0);
                while(mask != 0) {
                    game_phase += game_phase_inc[p];
                    int index = p*128 + BitboardHelper.ClearAndGetIndexOfLSB(ref mask)*2 + i*768;
                    mg += pestos_eval[index];
                    eg += pestos_eval[index+1];
                }
            }
            mg = -mg;
            eg = -eg;
        }

        game_phase = Math.Max(game_phase, 24);
        int eg_phase = 24 - game_phase;
        
        return (mg * game_phase + eg * eg_phase) / 24 * (board.IsWhiteToMove? 1 : -1) ;
    }

    int quiescence(int alpha, int beta) {
        int stand_pat = val();

        if(stand_pat >= beta) return beta;
        if(alpha < stand_pat) alpha = stand_pat;

        Move[] moves = board.GetLegalMoves(true);

        foreach(var move in moves) {
            board.MakeMove(move);
            int eval = -quiescence(-beta, -alpha);
            board.UndoMove(move);

            if(eval >= beta) return beta;
            if(eval > alpha) alpha = eval;
        }

        return alpha;
    }

    int search(int depth, int alpha, int beta, bool root, Timer timer) {
        if(board.IsInCheckmate()) return -100000001;
        if(board.IsDraw()) return 0;

        ulong hash = board.ZobristKey;
        TTable entry = table[hash % entries];

        if(entry.hash == hash && 
           !root && 
           entry.depth >= depth &&
           (entry.bound == 3 ||
            entry.bound == 2 && entry.score >= beta ||
            entry.bound == 1 && entry.score <= alpha)) return entry.score; 

        if(depth == 0) return quiescence(alpha, beta); 

        Move best_move = Move.NullMove;

        var moves = board.GetLegalMoves();

        Tuple<int, int>[] sorted = new Tuple<int, int>[moves.Length];

        //Add attacking pieces to sorting

        for(int i=0; i<moves.Length; i++) {
            if(entry.best_move == moves[i]) sorted[i] = (10000000, i).ToTuple();
            else if(moves[i].IsCapture) sorted[i] = (10 + (int)(moves[i].CapturePieceType - moves[i].MovePieceType), i).ToTuple();
            else sorted[i] = (0, i).ToTuple();
        }
        
        Array.Sort(sorted, (x, y) => y.Item1.CompareTo(x.Item1));

        int max_eval = -100000000, _alpha = alpha, eval, bound;
        foreach(var index in sorted) {
            if(timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30) return 10000000;
            Move move = moves[index.Item2];
            board.MakeMove(move);
            eval = -search(depth - 1, -beta, -alpha, false, timer);
            board.UndoMove(move);

            if(eval > max_eval) {
                max_eval = eval;
                best_move = move;
                if(root) 
                    last_best_move = move;

                alpha = Math.Max(eval, alpha);

                if (alpha >= beta) break;
            }
        }

        bound = max_eval>= beta ? 2 : max_eval > _alpha ? 3 : 1;
        table[hash % entries] = new TTable(hash, best_move, max_eval, depth, bound);

        return max_eval;
    }

    public Move Think(Board b, Timer timer)
    {
        board = b;

        last_best_move = Move.NullMove;
        Move best_move = last_best_move;

        // int[,] mg_eg_value = {{100, 310, 330, 500, 1000, 10000 }, {100, 310, 330, 500, 1000, 10000 }};

        // ulong mask = board.GetPieceBitboard((PieceType)6, true);
        // int index = BitboardHelper.ClearAndGetIndexOfLSB(ref mask);
        // Console.WriteLine(index + " " + pestos_eval[0, 5, 0, index]);
        // mask = board.GetPieceBitboard((PieceType)6, false);
        // index = BitboardHelper.ClearAndGetIndexOfLSB(ref mask);
        // Console.WriteLine(index + " " + pestos_eval[0, 5, 1, index]);

        for(int i=0; i<100; i++) {
            if(search(i, -100000000, 100000000, true, timer) != 10000000) best_move = last_best_move;

            if(timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30)
                break;
        }   

        return best_move == Move.NullMove? board.GetLegalMoves()[0] : best_move;
    }
}