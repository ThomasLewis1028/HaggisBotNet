using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace HaggisBotNet.Models
{
    public class IRoulette
    {
        public Int32 Round { get; set; }

        public long HighestCurrent => Players.OrderByDescending(p => p.CurrentStreak).First().Id;

        public long HighestTop => Players.OrderByDescending(p => p.HighestStreak).First().Id;

        public long HighestSurvives => Players.OrderByDescending(p => p.Survives).First().Id;

        public long HighestDeaths => Players.OrderByDescending(p => p.Deaths).First().Id;

        public Double HighestKD => Players.OrderByDescending(p => p.KillDeath).First().Id;

        public List<Player> Players { get; set; }

        public List<long> Played { get; set; }
        
        public DateTime LastPlayed { get; set; }
    }

    public class Player
    {
        public long Id { get; set; }

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
    }
}