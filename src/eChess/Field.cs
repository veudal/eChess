using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static eChess.Pieces;

namespace eChess
{
    public class Field
    {
        public Point Point { get; set; }
        
        public Piece Piece { get; set; }

        public bool DoubleMoved { get; set; }

        public bool FieldActivated { get; set; }
    }
}
