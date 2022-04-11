using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static eChess.Pieces;

namespace eChess
{
    internal class PortableGameNotation
    {
        public static void PGN_Writer(Field[,] board, Point currentPos, Point newPos, bool whitesTurn, ref int moveIndex, ref string PGN)
        {
            string pieceChar = string.Empty;
            Piece piece = board[currentPos.X, currentPos.Y].Piece;

            if (piece == Piece.WhiteKing || piece == Piece.BlackKing)
            {
                pieceChar = "K";
            }
            else if (piece == Piece.WhiteQueen || piece == Piece.BlackQueen)
            {
                pieceChar = "Q";
            }
            else if (piece == Piece.WhiteRook || piece == Piece.BlackRook)
            {
                pieceChar = "R";
            }
            else if (piece == Piece.WhiteBishop || piece == Piece.BlackBishop)
            {
                pieceChar = "B";
            }
            else if (piece == Piece.WhiteKnight || piece == Piece.BlackKnight)
            {
                pieceChar = "N";
            }

            string move = string.Empty;
            if (whitesTurn == true)
            {
                moveIndex++;
                move += moveIndex + ".";
            }
            move += " " + pieceChar + GetCoordinates(currentPos);
            if (board[newPos.X, newPos.Y].Piece != Piece.Empty)
            {
                move += "x";
            }
            move += GetCoordinates(newPos) + " ";
            PGN += move;
        }

        public static void PGN_Castling(string longOrShortCastle, ref string PGN)
        {
            PGN = PGN.Remove(PGN.Length - 1);
            int index = PGN.LastIndexOf(" ");
            PGN = PGN.Substring(0, index + 1);
            PGN += longOrShortCastle;
        }

        private static string GetCoordinates(Point point)
        {
            string[] letters = new string[8] { "a", "b", "c", "d", "e", "f", "g", "h" };
            int[] yCoordinates = new int[8] { 8, 7, 6, 5, 4, 3, 2, 1 };
            string coordinate = letters[point.X].ToString() + (yCoordinates[point.Y]).ToString();
            return coordinate;
        }
    }
}
