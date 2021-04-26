using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using HaggisBotNet.Games;
using Color = Discord.Color;

namespace HaggisBotNet
{
    internal class HaggisBot
    {
        // private static Games.HaggisBotNet.Games _games;
        private static Roulette _roulette;
        private static Betting _betting;
        private static RegularExpressions _regex = new RegularExpressions();

        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        // Properties file
        private static readonly JObject Prop =
            JObject.Parse(
                File.ReadAllText(@"properties.json"));

        // Get the token out of the properties folder
        private readonly string _token;
        private readonly long _gamesChannel;
        private readonly long _haggisId;

        private List<Int64> usersChatting = new List<long>();

        // Discord config files
        private DiscordSocketClient _client;

        public HaggisBot(bool test)
        {
            _token =
                test ? (string) Prop.GetValue("tokenTest") : (string) Prop.GetValue("token");

            _gamesChannel = (long) Prop.GetValue("gamesChannel");

            _haggisId = (long) Prop.GetValue("Admin")[0].First.First;

            _roulette = new Roulette(GamePath);

            _betting = new Betting(GamePath);

            // _dmChannel =
            //     test ? (long) Prop.GetValue("Test DM") : (long) Prop.GetValue("DM Channel");
        }

        private static string GamePath
        {
            get
            {
                var codeBase = Assembly.GetExecutingAssembly().CodeBase;
                var uri = new UriBuilder(codeBase!);
                var path = Uri.UnescapeDataString(uri.Path);
                if (!Directory.Exists(Path.GetDirectoryName(path) + "/GameData"))
                    Directory.CreateDirectory(Path.GetDirectoryName(path) + "/GameData");
                return Path.GetDirectoryName(path) + "/GameData";
            }
        }

        public async Task MainAsync()
        {
            var config = new DiscordSocketConfig {MessageCacheSize = 100};
            // _client.Log += message => Console.Out.WriteLine();
            _client = new DiscordSocketClient(config);
            _client.MessageReceived += MessageReceived;
            // _client.ReactionAdded += ReactionAdded;

            await _client.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();
            await _client.SetGameAsync("Ruining lives since the Nineteen Ninety..Fives..");

            _logger.Info("DiscordBot Connected");

            while (true)
            {
                usersChatting = _betting.AddPoints(usersChatting);
                await Task.Delay(180000);
            }
        }

        private async Task MessageReceived(SocketMessage sm)
        {
            if (sm.Author.IsBot)
                return;

            if (sm.Content.ToLower() == "ping")
                await sm.Channel.SendMessageAsync("Pong");
            else if (sm.Content.ToLower() == "pong")
                await sm.Channel.SendMessageAsync("Ping");

            try
            {
                switch (sm.Content)
                {
                    case var content when _regex.Subreddit.IsMatch(content) && !_regex.Reddit.IsMatch(content):
                        await sm.Channel.SendMessageAsync(
                            @"https://www.reddit.com/r/" +
                            content.Split(' ').Single(m => m.Contains(@"r/")).Split(@"r/")[1]);
                        break;
                    case var content when _regex.TempConv.IsMatch(content):
                        await ((IUserMessage) sm).ReplyAsync(TemperatureConversion.Convert(content));
                        break;
                    case var content when _regex.Help.IsMatch(content):
                        await SendHelp(sm);
                        break;
                    case var content when _regex.CreateBet.IsMatch(content):
                        await ((IUserMessage) sm).ReplyAsync(_betting.CreateBet(sm));
                        break;
                    case var content when _regex.EndBet.IsMatch(content):
                        await ((IUserMessage) sm).ReplyAsync(_betting.EndBet(sm));
                        break;
                    case var content when _regex.AddBet.IsMatch(content):
                        await ((IUserMessage) sm).ReplyAsync(_betting.AddBet(sm));
                        break;
                    case var content when _regex.ViewBet.IsMatch(content):
                        var betReturn = _betting.ViewBet(sm);
                        await ((IUserMessage) sm).ReplyAsync(betReturn.Item1, false, betReturn.Item2);
                        break;
                    case var content when _regex.ListBets.IsMatch(content):
                        var betListReturn = _betting.ListBets(sm);
                        await ((IUserMessage) sm).ReplyAsync(betListReturn.Item1, false, betListReturn.Item2);
                        break;
                    case var content when _regex.ViewPlayer.IsMatch(content):
                        var playerReturn = _betting.ViewPlayer(sm);
                        await ((IUserMessage) sm).ReplyAsync(playerReturn.Item1, false, playerReturn.Item2);
                        break;
                    case var content when _regex.EditBet.IsMatch(content):
                        await ((IUserMessage) sm).ReplyAsync(_betting.EditBet(sm));
                        break;
                    case var content when _regex.RollDice.IsMatch(content):
                        await ((IUserMessage) sm).ReplyAsync(RollDice(sm));
                        break;
                }
            }
            catch (Exception e)
            {
                _logger.Error(e);
                await sm.Channel.SendMessageAsync(e.Message);
            }


            if ((long) sm.Channel.Id == _gamesChannel)
                try
                {
                    switch (sm.Content)
                    {
                        case var content when _regex.RouletteRegex.IsMatch(content):
                            _logger.Info("Playing round of roulette: " + content);
                            await ((IUserMessage) sm).ReplyAsync(_roulette.PlayRound(sm));
                            break;
                        case var content when _regex.RouletteStatsRegex.IsMatch(content):
                            _logger.Info("Getting roulette stats: " + content);
                            var statsReturn = _roulette.GetStats((Int64) sm.Author.Id);
                            await ((IUserMessage) sm).ReplyAsync(statsReturn.Item1, false, statsReturn.Item2);
                            break;
                        case var content when _regex.RouletteLeadRegex.IsMatch(content):
                            _logger.Info("Getting roulette leaderboard: " + content);
                            var leadReturn = _roulette.GetLeaders((long) sm.Author.Id);
                            await ((IUserMessage) sm).ReplyAsync(leadReturn.Item1, false, leadReturn.Item2);
                            break;
                        case var content when _regex.RouletteSpin.IsMatch(content):
                            _logger.Info("Spinning roulette barrel: " + content);
                            await ((IUserMessage) sm).ReplyAsync(_roulette.SpinBarrel());
                            break;
                        // case var content when _regex.RouletteWhip.IsMatch(content):
                        //     _logger.Info("Pistol Whipping Roullete: " + content);
                        //     _roulette.PistolWhip(sm).RunSynchronously();
                        //     break;
                        // case var content when _regex.RouletteWhipCounter.IsMatch(content):
                        //     _logger.Info("Counter Whipping Roulette: " + content);
                        //     await sm.Channel.SendMessageAsync(_roulette.CounterWhip(sm));
                        //     break;
                        case var content when _regex.RouletteShootPlayer.IsMatch(content):
                            _logger.Info("Shooting Player Roulette: " + content);
                            await ((IUserMessage) sm).ReplyAsync(_roulette.ShootPlayer(sm));
                            break;
                    }
                }
                catch (Exception e)
                {
                    _logger.Error(e);
                    await sm.Channel.SendMessageAsync(e.Message);
                }

            if (!usersChatting.Contains((Int64) sm.Author.Id)) usersChatting.Add((Int64) sm.Author.Id);
        }

        private async Task SendHelp(SocketMessage sm)
        {
            EmbedBuilder eb = new EmbedBuilder();
            eb.Title = "Help";
            eb.Description = "All commands are case insensitive and are preceded with !\n" +
                             "<> indicates required parameter \n" +
                             "[] indicates optional parameter \n" +
                             "() indicates either or parameter";
            eb.Color = Color.Gold;
            eb.AddField("Help", "help");
            eb.AddField("Ping", "Pong");
            eb.AddField("Convert Temperatures", "temp <Temperature><Unit>");
            eb.AddField("Roll Dice", "r <numDice>d<diceSides>[(+ | - | / | *)<mod>]");

            eb.AddField("Betting",
                "Create Bet - (createBet | betCreate | cb) <bet title>\n" +
                "End Bet - (endBet | betEnd | eb) <bet Id> <winning value>\n" +
                "Bet - (bet | addBet | betAdd) <bet Id> <bet value> <bet points>\n" +
                "List Bets - (listBets | betsList | lb) [-all]\n" +
                "View Bet - (viewBet | betView | vb) <bet Id>\n" +
                "View Player - (viewPlayer | playerView | vp) [@<user>]");

            eb.AddField("Roulette",
                "Play Roulette - rr\n" +
                "Roulette Stats - (rrStats | rrS)\n" +
                "Roulette Leaderboard - (rrLB | rrLeaderBoard | rrLead)\n" +
                "Roulette Spin - rrSpin\n" +
                // "Roulette Pistol Whip - (rrPistolWhip | rrWhip | rrPW) @<user>\n" +
                // "Roulette Counter Whip - rrCounterWhip | rrCW\n" +
                "Roulette Shoot Player - (rrSP | rrShootPlayer) <@<user>>");

            _logger.Info("Sending help list: " + sm.Content);
            await sm.Channel.SendMessageAsync(null, false, eb.Build());
        }

        private String RollDice(SocketMessage sm)
        {
            var contents = sm.Content.Split(' ')[1];
            var roll = contents.Split('d', '+', '-', '*', '/');
            var rolls = new List<int>();
            var total = 0;

            if (Int32.Parse(roll[0]) > 100)
                return "Roll less than 100 dice";

            for (int i = 0; i < Int32.Parse(roll[0]); i++)
            {
                var random = new Random().Next(1, Int32.Parse(roll[1]) + 1);
                rolls.Add(random);
                total += random;
            }
            
            var result = roll.Length == 2 ? total
                : contents.Contains('+') ? total + Int32.Parse(roll[2])
                : contents.Contains('-') ? total - Int32.Parse(roll[2])
                : contents.Contains('*') ? total * Int32.Parse(roll[2])
                : contents.Contains('/') ? total / Int32.Parse(roll[2])
                : total;

            return $"Rolled {contents} and got {result}\n" +
                   $"`[{string.Join(", ",rolls)}]`";
        }
    }
}