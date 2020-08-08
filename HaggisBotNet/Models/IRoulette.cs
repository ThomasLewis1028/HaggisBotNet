using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace HaggisBotNet.Models
{
    public class IRoulette
    {
        public Int32 Round { get; set; }

        public Int64 HighestCurrent => Players.OrderByDescending(p => p.CurrentStreak).First().Id;

        public Int64 HighestTop => Players.OrderByDescending(p => p.HighestStreak).First().Id;

        public Int64 HighestSurvives => Players.OrderByDescending(p => p.Survives).First().Id;

        public Int64 HighestDeaths => Players.OrderByDescending(p => p.Deaths).First().Id;

        public Int64 HighestKD => Players.OrderByDescending(p => p.KillDeath).First().Id;

        public List<Player> Players { get; set; }

        public List<Int64> Played { get; set; }
        
        public DateTime LastPlayed { get; set; }
    }

    public class Player
    {
        public Int64 Id { get; set; }

        public String Name { get; set; }

        public Int32 HighestStreak { get; set; }

        public Int32 CurrentStreak { get; set; }

        public Int32 Survives { get; set; }

        public Int32 Deaths { get; set; }

        [JsonIgnore]
        public Double KillDeath => Survives == 0
            ? 0
            : Deaths == 0
                ? Survives
                : (Double) Survives / Deaths;
        
        public DateTime LastPistolWhip { get; set; }
        
        public Boolean Whipped { get; set; }
    }
}