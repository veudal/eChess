using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace eChess
{
    internal class GameEntity
    {
        public bool White { get; set; }
        public string OpponentName { get; set; }
        public Guid GameID { get; set; } 
    }
}

