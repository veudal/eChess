using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static eChess.Pieces;

namespace eChess
{
    internal class Check
    {
        static readonly Moves moves = new Moves();

        static public bool IsCheck(Field[,] board, bool whitesTurn, bool currentlyInCheck)
        {
            List<Piece> pieces = new List<Piece> { Piece.WhiteBishop, Piece.WhiteKnight, Piece.WhitePawn, Piece.WhiteQueen, Piece.WhiteRook, Piece.WhiteKing };
            if (whitesTurn == false)
                pieces = new List<Piece> { Piece.BlackBishop, Piece.BlackKnight, Piece.BlackPawn, Piece.BlackQueen, Piece.BlackRook, Piece.BlackKing };


            Piece king = Piece.BlackKing;
            if (whitesTurn == false)
                king = Piece.WhiteKing;

            foreach (var piece in pieces)
            {
                var allPiecesOfOneType = GetPiecePos(board, piece);
                foreach (var pos in allPiecesOfOneType)
                {
                    var result = moves.GetValidMoves(piece, pos, board, whitesTurn, true, currentlyInCheck);
                    if (result.Contains(GetPiecePos(board, king).FirstOrDefault()))
                    {
                        //Check detected
                        return true;
                    }
                }
            }

            return false;
        }
        static public List<Point> GetPiecePos(Field[,] board, Piece piece)
        {
            List<Point> allPiecesOfOneType = new List<Point>();
            foreach (var field in board)
            {
                if (field.Piece == piece)
                {
                    allPiecesOfOneType.Add(field.Point);
                }
            }
            return allPiecesOfOneType;
        }
    }
}
