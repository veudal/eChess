using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static eChess.Pieces;

namespace eChess
{
    internal class Moves
    {
        bool whitesTurn;
        bool preview;
        bool currentlyInCheck;

        public List<Point> GetValidMoves(Piece piece, Point pos, Field[,] board, bool whitesTurn, bool preview, bool currentlyInCheck)
        {
            this.whitesTurn = whitesTurn;
            this.preview = preview;
            this.currentlyInCheck = currentlyInCheck;
            List<Point> validMoves = new List<Point>();
            GetMovesForSelectedPiece(piece, pos, board, validMoves);
            return validMoves;
        }

        private void GetMovesForSelectedPiece(Piece piece, Point pos, Field[,] board, List<Point> validMoves)
        {
            List<Piece> pieces = new List<Piece> { Piece.WhiteBishop, Piece.WhiteKnight, Piece.WhitePawn, Piece.WhiteQueen, Piece.WhiteRook, Piece.WhiteKing };
            int pawnY = 1;
            string color = "White";

            if (whitesTurn == false)
            {
                pieces = new List<Piece> { Piece.BlackBishop, Piece.BlackKnight, Piece.BlackPawn, Piece.BlackQueen, Piece.BlackRook, Piece.BlackKing };
                pawnY = -1;
                color = "Black";
            }

            if ((piece == Piece.BlackPawn || piece == Piece.WhitePawn) && piece.ToString().Contains(color))
            {
                Pawn(pos, board, validMoves, pieces, pawnY);
            }
            else if ((piece == Piece.BlackKnight || piece == Piece.WhiteKnight) && piece.ToString().Contains(color))
            {
                Knight(pos, board, validMoves, pieces);
            }
            else if ((piece == Piece.BlackKing || piece == Piece.WhiteKing) && piece.ToString().Contains(color))
            {
                King(pos, board, validMoves, pieces);
            }
            else if ((piece == Piece.BlackRook || piece == Piece.WhiteRook) && piece.ToString().Contains(color))
            {
                Rook(pos, board, validMoves, pieces);
            }
            else if ((piece == Piece.BlackBishop || piece == Piece.WhiteBishop) && piece.ToString().Contains(color))
            {
                Bishop(pos, board, validMoves, pieces);
            }
            else if ((piece == Piece.BlackQueen || piece == Piece.WhiteQueen) && piece.ToString().Contains(color))
            {
                Queen(pos, board, validMoves, pieces);
            }
        }

        private void Queen(Point pos, Field[,] board, List<Point> validMoves, List<Piece> pieces)
        {
            Bishop(pos, board, validMoves, pieces);
            Rook(pos, board, validMoves, pieces);
        }

        private void Bishop(Point pos, Field[,] board, List<Point> validMoves, List<Piece> pieces)
        {
            List<Point> points = new List<Point>
            {
                //Up left(from whites perspective)
                new Point(-1, -1),

                //Up right (from whites perspective)
                new Point(1, -1),

                //Down right (from whites perspective)
                new Point(1, 1),

                //Down left (from whites perspective)
                new Point(-1, 1)
            };

            foreach (var point in points)
            {
                BishopMoves(new Point(pos.X, pos.Y), board, validMoves, pieces, point.X, point.Y);
            }
        }

        private void BishopMoves(Point pos, Field[,] board, List<Point> validMoves, List<Piece> pieces, int east, int south)
        {
            Point move = pos;
            while (true)
            {
                move.Y += south;
                move.X += east;
                if (InsideBoard(move))
                {
                    if (LegalMove(move, board, pos))
                    {
                        if (!pieces.Contains(board[move.X, move.Y].Piece))
                        {
                            validMoves.Add(new Point(move.X, move.Y));
                            if (board[move.X, move.Y].Piece != Piece.Empty)
                            {
                                //Needs to stop because it can not jump over a enemy piece
                                break;
                            }
                        }
                        else
                            break;
                    }
                    else
                    {
                        if (board[move.X, move.Y].Piece != Piece.Empty)
                        {
                            break;
                        }
                    }
                }
                else
                    break;
            }
        }

        private void Rook(Point pos, Field[,] board, List<Point> validMoves, List<Piece> pieces)
        {
            List<Point> points = new List<Point>
            {
              
                //Up (from whites perspective)
                new Point(0, -1),

                //Right (from whites perspective)
                new Point(1, 0),

                //Down (from whites perspective)
                new Point(0, 1),

                //Left (from whites perspective)
                new Point(-1, 0)
            };

            foreach (var point in points)
            {
                RookMoves(new Point(pos.X, pos.Y), board, validMoves, pieces, point.X, point.Y);
            }

        }

        private bool LegalMove(Point move, Field[,] board, Point pos)
        {
            if (preview)
            {
                return true;
            }
            Field[,] previewBoard = board.Clone<Field[,]>();
            previewBoard[move.X, move.Y].Piece = previewBoard[pos.X, pos.Y].Piece;
            previewBoard[pos.X, pos.Y].Piece = Piece.Empty;
            if (Check.IsCheck(previewBoard, !whitesTurn, currentlyInCheck) == false)
            {
                return true;
            }
            return false;
        }

        private bool InsideBoard(Point move)
        {
            if (move.Y >= 0 && move.Y <= 7 && move.X >= 0 && move.X <= 7)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void RookMoves(Point move, Field[,] board, List<Point> validMoves, List<Piece> pieces, int east, int south)
        {
            Point pos = move;
            while (true)
            {
                if (south != 0)
                    move.Y += south;

                else
                    move.X += east;

                if (InsideBoard(move))
                {
                    if (LegalMove(move, board, pos))
                    {
                        if (!pieces.Contains(board[move.X, move.Y].Piece))
                        {
                            validMoves.Add(new Point(move.X, move.Y));
                            if (board[move.X, move.Y].Piece != Piece.Empty)
                            {
                                //Needs to stop because it can not jump over a enemy piece
                                break;
                            }
                        }
                        else
                            break;
                    }
                    else
                    {
                        if (board[move.X, move.Y].Piece != Piece.Empty)
                        {
                            break;
                        }
                    }
                }
                else
                    break;

            }
        }

        private void King(Point pos, Field[,] board, List<Point> validMoves, List<Piece> pieces)
        {
            List<Point> possibleMoves = new List<Point>
            {

                //Top left (from whites perspective)
                new Point(pos.X - 1, pos.Y - 1),

                //Top middle (from whites perspective)
                new Point(pos.X, pos.Y - 1),

                //Top right (from whites perspective)
                new Point(pos.X + 1, pos.Y - 1),

                //Right middle (from whites perspective)
                new Point(pos.X + 1, pos.Y),

                 //Bottom right (from whites perspective)
                 new Point(pos.X + 1, pos.Y + 1),

                 //Bottom middle (from whites perspective)
                 new Point(pos.X, pos.Y + 1),

                 //Bottom left (from whites perspective)
                 new Point(pos.X - 1, pos.Y + 1),
            
                 //Left middle (from whites perspective)
                 new Point(pos.X - 1, pos.Y)


             };

            foreach (var move in possibleMoves)
            {
                IsValidMove(board, validMoves, pieces, move, pos);
            }

            ShortCastle(pos, board, validMoves);
            LongCastle(pos, board, validMoves);
        }

        private void ShortCastle(Point pos, Field[,] board, List<Point> validMoves)
        {
            //Check if there is check, king and the rook moved yet and if there are pieces between them
            if (currentlyInCheck == false && board[pos.X, pos.Y].FieldActivated == false && board[pos.X + 3, pos.Y].FieldActivated == false && board[pos.X + 1, pos.Y].Piece == Piece.Empty && board[pos.X + 2, pos.Y].Piece == Piece.Empty)
            {
                //Check if king would need to go through check or is in check after castling
                if (LegalMove(new Point(pos.X + 1, pos.Y), board, pos) && LegalMove(new Point(pos.X + 2, pos.Y), board, pos))
                {
                    validMoves.Add(new Point(pos.X + 2, pos.Y));
                }
            }
        }

        private void LongCastle(Point pos, Field[,] board, List<Point> validMoves)
        {
            //Check if there is check, king and the rook moved yet and if there are pieces between them
            if (currentlyInCheck == false && board[pos.X, pos.Y].FieldActivated == false && board[pos.X - 4, pos.Y].FieldActivated == false && board[pos.X - 1, pos.Y].Piece == Piece.Empty && board[pos.X - 2, pos.Y].Piece == Piece.Empty && board[pos.X - 3, pos.Y].Piece == Piece.Empty)
            {
                //Check if king would need to go through check or is in check after castling
                if (LegalMove(new Point(pos.X - 1, pos.Y), board, pos) && LegalMove(new Point(pos.X - 2, pos.Y), board, pos))
                {
                    validMoves.Add(new Point(pos.X - 2, pos.Y));
                }
            }
        }

        private void Knight(Point pos, Field[,] board, List<Point> validMoves, List<Piece> pieces)
        {
            List<Point> possibleMoves = new List<Point>
            {

                //Top left jump (from whites perspective)
                new Point(pos.X - 1, pos.Y - 2),

                //Top right jump (from whites perspective)
                new Point(pos.X + 1, pos.Y - 2),

                //Right up jump (from whites perspective)
                new Point(pos.X + 2, pos.Y - 1),

                //Right down jump (from whites perspective)
                new Point(pos.X + 2, pos.Y + 1),

                //Left up jump (from whites perspective)
                new Point(pos.X - 2, pos.Y - 1),

                //Left down jump (from whites perspective)
                new Point(pos.X - 2, pos.Y + 1),

                //Down left jump (from whites perspective)
                new Point(pos.X - 1, pos.Y + 2),

                //Down right jump (from whites perspective)
                new Point(pos.X + 1, pos.Y + 2)
            };


            foreach (var move in possibleMoves)
            {
                IsValidMove(board, validMoves, pieces, move, pos);
            }
        }

        private void IsValidMove(Field[,] board, List<Point> validMoves, List<Piece> pieces, Point move, Point pos)
        {
            if (InsideBoard(move) && !pieces.Contains(board[move.X, move.Y].Piece) && LegalMove(move, board, pos))
            {
                validMoves.Add(move);
            }
        }

        private void Pawn(Point pos, Field[,] board, List<Point> validMoves, List<Piece> pieces, int y)
        {
            int startPos = 6;
            if (y == -1)
                startPos = 1;

            Point move;

            //One forward
            move = new Point(pos.X, pos.Y - 1 * y);

            if (InsideBoard(move))
            {

                if (LegalMove(move, board, pos) && board[move.X, move.Y].Piece == Piece.Empty)
                {
                    validMoves.Add(move);
                }
                if (board[move.X, move.Y].Piece == Piece.Empty)
                {
                    //Double move at the start position
                    move = new Point(pos.X, pos.Y - 2 * y);
                    if (InsideBoard(move) && LegalMove(move, board, pos) && pos.Y == startPos && board[move.X, move.Y].Piece == Piece.Empty && board[move.X, move.Y].Piece == Piece.Empty)
                    {
                        validMoves.Add(move);
                    }
                }
            }

            //To make sure that pawn does not move diagonally when there is no piece on that field
            pieces.Add(Piece.Empty);

            //Capture right top piece (from whites perspective)
            move = new Point(pos.X + 1 * y, pos.Y - 1 * y);
            if (InsideBoard(move) && LegalMove(move, board, pos) && !pieces.Contains(board[move.X, move.Y].Piece))
            {
                validMoves.Add(move);
            }
            //En passent right top (from whites perspective)
            else if (InsideBoard(move) && LegalMove(move, board, pos) && board[move.X, move.Y + y * 1].DoubleMoved == true)
            {
                validMoves.Add(move);
            }

            //Capture left top piece (from whites perspective)
            move = new Point(pos.X - 1 * y, pos.Y - 1 * y);
            if (InsideBoard(move) && LegalMove(move, board, pos) && !pieces.Contains(board[move.X, move.Y].Piece))
            {
                validMoves.Add(move);
            }
            //En passent left top (from whites perspective)
            else if (InsideBoard(move) && LegalMove(move, board, pos) && board[move.X, move.Y + y * 1].DoubleMoved == true)
            {
                validMoves.Add(move);
            }
        }
    }
}

