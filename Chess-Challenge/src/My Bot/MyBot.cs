using ChessChallenge.API;
using System;

public class MyBot : IChessBot
{
    Board board;
    Move bestMove;

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

    int[] values = {1, 3, 3, 5, 9};
    int val(bool white) {
        int count = 0;
        for(var i=1; i < 6; i++) {
            count += board.GetPieceList((PieceType)i, white).Count * values[i-1];
        }
        return count;
    }

    int evaluation() {
        return val(true) - val(false);
    }

    int quiescence(int alpha, int beta) {
        int stand_pat = evaluation();
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

            if(entry.best_move == moves[i]) scores[i] = ((int)1e7, i).ToTuple();
            else if(moves[i].IsCapture) scores[i] = (7 + (int)moves[i].CapturePieceType - (int)moves[i].MovePieceType, i).ToTuple();
            else scores[i] = (0, i).ToTuple();
        }
        
        Array.Sort(scores, (x, y) => y.Item1.CompareTo(x.Item1));

        return scores;
    }

    int search(int depth, int alpha, int beta, bool root) {
        if(board.IsInCheckmate()) return (int)-1e7;
        else if(board.IsInStalemate()) return 0;

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

        if(root) bestMove = moves[0];

        int _alpha = alpha;

        int max_eval = (int)-1e7;
        foreach(var index in sorted) {
            board.MakeMove(moves[index.Item2]);
            int eval = -search(depth - 1, -beta, -alpha, false);
            board.UndoMove(moves[index.Item2]);

            if(eval > max_eval) {
                max_eval = eval;
                if(root) 
                    bestMove = moves[index.Item2];
                if(eval > alpha)
                    alpha = eval;

                if (alpha >= beta) break;

            }
        }

        int bound = max_eval>= beta ? 2 : max_eval > _alpha ? 3 : 1;
        table[hash % entries] = new TTable(hash, bestMove, max_eval, depth, bound);

        return max_eval;
    }

    public Move Think(Board b, Timer timer)
    {
        board = b;

        int i = 0;
        for(; i<100; i++) {
            search(i, (int)-1e7, (int)1e7, true);

            if(timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 100)
                break;
        }   

        Console.WriteLine(i);

        return bestMove;
    }
}