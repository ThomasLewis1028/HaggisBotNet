using System;
using System.Collections.Generic;

namespace HaggisBotNet.Models
{
    public class IBetting
    {
        public List<Better> Betters { get; set; }
        
        public List<Bet> Bets { get; set; }
    }

    public class Bet
    {
        public String BetName { get; set; }
        
        public Int32 BetId { get; set; }

        public Int64 Bookie { get; set; }
        
        public Boolean IsActive { get; set; }

        public Int32 WinningBet { get; set; }
    }

    public class PlayerBet
    {
        public Int64 BetterId { get; set; }
        
        public Int32 BetId { get; set; }
        
        public Int32 Bet { get; set; }
    }

    public class Better
    {
        public Int64 BetterId { get; set; }

        public Int32 Bet { get; set; }

        public Int32 Points { get; set; }
        
        public List<PlayerBet> Bets { get; set; }
    }
}