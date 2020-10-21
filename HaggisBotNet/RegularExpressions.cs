using System.Text.RegularExpressions;

namespace HaggisBotNet
{
    public class RegularExpressions
    {
        // Roulette-related regex
        public readonly Regex RouletteRegex =
            new Regex("^!(rr)($| .*)", RegexOptions.IgnoreCase);

        public readonly Regex RouletteStatsRegex =
            new Regex("^!(rrStats|rrS)($| .*)", RegexOptions.IgnoreCase);

        public readonly Regex RouletteLeadRegex =
            new Regex("^!(rrLB|rrLeaderBoard|rrLead)($| .*)", RegexOptions.IgnoreCase);

        public readonly Regex RouletteSpin =
            new Regex("^!(rrSpin)($| .*)", RegexOptions.IgnoreCase);

        public readonly Regex RouletteWhip =
            new Regex("^!(rrPistolWhip|rrWhip|rrPW) <@!(\\d+)>($| .*)", RegexOptions.IgnoreCase);

        public readonly Regex RouletteWhipCounter =
            new Regex("^!(rrCounterWhip|rrCW)", RegexOptions.IgnoreCase);

        public readonly Regex RouletteShootPlayer =
            new Regex("^!(rrShootPlayer|rrSP) <@!(\\d+)>($| .*)", RegexOptions.IgnoreCase);
        
        // Help regex
        public readonly Regex Help =
            new Regex("^!(help)", RegexOptions.IgnoreCase);

        // Conversion regex
        public readonly Regex TempConv = new Regex("^!temp -?\\d+(.\\d+|)(c|f)$", RegexOptions.IgnoreCase);

        // Subreddit regex
        public readonly Regex Subreddit = new Regex("(^| |^/| /)r/[^/ ]+", RegexOptions.IgnoreCase);
        public readonly Regex Reddit = new Regex("(com)", RegexOptions.IgnoreCase);

        // Bet-related regex
        public readonly Regex CreateBet = new Regex("^!(createBet|betCreate|cb) (.*)$", RegexOptions.IgnoreCase);
        public readonly Regex EndBet = new Regex("^!(endBet|betEnd|eb) \\d* \\d*$", RegexOptions.IgnoreCase);
        public readonly Regex AddBet = new Regex("^!(bet|addBet|betAdd) \\d* \\d* \\d*$", RegexOptions.IgnoreCase);
        public readonly Regex ListBets = new Regex("^!(listBets|betsList|lb)(| -all)$", RegexOptions.IgnoreCase);
        public readonly Regex ViewBet = new Regex("^!(viewBet|betView|vb) \\d.*$", RegexOptions.IgnoreCase);
    }
}