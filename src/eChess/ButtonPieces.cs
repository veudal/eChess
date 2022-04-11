using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using static eChess.Pieces;

namespace eChess
{
    internal class ButtonPieces
    {
        public static Dictionary<Button, Point> GetPositions(Button BB1, Button BB2, Button BK, Button BN1, Button BN2, Button BQ, Button BR1, Button BR2, Button BP1, Button BP2, Button BP3, Button BP4, Button BP5, Button BP6, Button BP7, Button BP8, Button WB1, Button WB2, Button WK, Button WN1, Button WN2, Button WQ, Button WR1, Button WR2, Button WP1, Button WP2, Button WP3, Button WP4, Button WP5, Button WP6, Button WP7, Button WP8)
        {
            return new Dictionary<Button, Point>()
            {
                { BB1, new Point(5, 0) },
                { BB2, new Point(2, 0) },
                { BK, new Point(4, 0) },
                { BN1, new Point(6, 0) },
                { BN2, new Point(1, 0) },
                { BQ, new Point(3, 0) },
                { BR1, new Point(0, 0) },
                { BR2, new Point(7, 0) },
                { BP1, new Point(0, 1) },
                { BP2, new Point(1, 1) },
                { BP3, new Point(2, 1) },
                { BP4, new Point(3, 1) },
                { BP5, new Point(4, 1) },
                { BP6, new Point(5, 1) },
                { BP7, new Point(6, 1) },
                { BP8, new Point(7, 1) },
                { WB1, new Point(2, 7) },
                { WB2, new Point(5, 7) },
                { WK, new Point(4, 7) },
                { WN1, new Point(1, 7) },
                { WN2, new Point(6, 7) },
                { WQ, new Point(3, 7) },
                { WR1, new Point(0, 7) },
                { WR2, new Point(7, 7) },
                { WP1, new Point(0, 6) },
                { WP2, new Point(1, 6) },
                { WP3, new Point(2, 6) },
                { WP4, new Point(3, 6) },
                { WP5, new Point(4, 6) },
                { WP6, new Point(5, 6) },
                { WP7, new Point(6, 6) },
                { WP8, new Point(7, 6) },
            };
        }

        public static Dictionary<Button, Piece> GetPieces(Button BB1, Button BB2, Button BK, Button BN1, Button BN2, Button BQ, Button BR1, Button BR2, Button BP1, Button BP2, Button BP3, Button BP4, Button BP5, Button BP6, Button BP7, Button BP8, Button WB1, Button WB2, Button WK, Button WN1, Button WN2, Button WQ, Button WR1, Button WR2, Button WP1, Button WP2, Button WP3, Button WP4, Button WP5, Button WP6, Button WP7, Button WP8)
        {
            return new Dictionary<Button, Piece>()
            {
                { BB1, Piece.BlackBishop },
                { BB2, Piece.BlackBishop },
                { BK, Piece.BlackKing},
                { BN1, Piece.BlackKnight},
                { BN2, Piece.BlackKnight},
                { BQ, Piece.BlackQueen},
                { BR1, Piece.BlackRook},
                { BR2, Piece.BlackRook},
                { BP1, Piece.BlackPawn},
                { BP2, Piece.BlackPawn},
                { BP3, Piece.BlackPawn},
                { BP4, Piece.BlackPawn},
                { BP5, Piece.BlackPawn},
                { BP6, Piece.BlackPawn},
                { BP7, Piece.BlackPawn},
                { BP8, Piece.BlackPawn},
                { WB1, Piece.WhiteBishop},
                { WB2, Piece.WhiteBishop},
                { WK, Piece.WhiteKing},
                { WN1, Piece.WhiteKnight},
                { WN2, Piece.WhiteKnight},
                { WQ, Piece.WhiteQueen},
                { WR1, Piece.WhiteRook},
                { WR2, Piece.WhiteRook},
                { WP1, Piece.WhitePawn},
                { WP2, Piece.WhitePawn},
                { WP3, Piece.WhitePawn},
                { WP4, Piece.WhitePawn},
                { WP5, Piece.WhitePawn},
                { WP6, Piece.WhitePawn},
                { WP7, Piece.WhitePawn},
                { WP8, Piece.WhitePawn},
            };
        }
    }
}
