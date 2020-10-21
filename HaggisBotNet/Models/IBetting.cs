using System;
using System.Collections.Generic;

namespace HaggisBotNet.Models
{
    public class IBetting
    {
        public List<Better> Betters { get; set; }
        
        public List<Bet> Bets { get; set; }
        
        public List<PlayerBet> PlayerBets { get; set; }
    }

    public class Bet
    {
        public String Name { get; set; }
        
        public Int32 Id { get; set; }

        public Int64 BookieId { get; set; }
        
        public String BookieName { get; set; }
        
        public Boolean IsActive { get; set; }

        public Int32 WinningBet { get; set; }
        
        public Int32 BetPool { get; set; }
        
        public DateTime StartDate { get; set; }
        
        public DateTime CloseDate { get; set; }
    }

    public class PlayerBet
    {
        public Int64 BetterId { get; set; }
        
        public Int32 BetId { get; set; }
        
        public Int32 Bet { get; set; }
        
        public Int32 Points { get; set; }
    }

    public class Better
    {
        public Int64 Id { get; set; }
        
        public String Name { get; set; }

        public Int32 Points { get; set; }
        
        public Int32 BetsWon { get; set; }
        
        public Dictionary<Int32, String> WonBetsList { get; set; }
    }
}