using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Games.HaggisBotNet;
using Newtonsoft.Json.Linq;

namespace HaggisBotNet
{
    internal class HaggisBot
    {
        // private static Games.HaggisBotNet.Games _games;
        private static Roulette _roulette;

        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        // Properties file
        private static readonly JObject Prop =
            JObject.Parse(
                File.ReadAllText(@"properties.json"));

        // Get the token out of the properties folder
        private readonly string _token;
        private readonly long _gamesChannel;
        private readonly long _haggisId;

        private readonly Regex _rouletteRegex =
            new Regex("^!(rr)($| .*)", RegexOptions.IgnoreCase);

        private readonly Regex _rouletteStatsRegex =
            new Regex("^!(rrStats|rrS)($| .*)", RegexOptions.IgnoreCase);

        private readonly Regex _rouletteLeadRegex =
            new Regex("^!(rrLB|rrLeaderBoard|rrLead)($| .*)", RegexOptions.IgnoreCase);

        private readonly Regex _rouletteSpin =
            new Regex("^!(rrSpin)($| .*)", RegexOptions.IgnoreCase);

        private readonly Regex _rouletteWhip =
            new Regex("^!(rrPistolWhip|rrWhip|rrPW) <@!(\\d+)>($| .*)", RegexOptions.IgnoreCase);

        private readonly Regex _rouletteWhipCounter =
            new Regex("^!(rrCounterWhip|rrCW)", RegexOptions.IgnoreCase);

        private readonly Regex _rouletteShootPlayer =
            new Regex("^!(rrShootPlayer|rrSP) <@!(\\d+)>($| .*)", RegexOptions.IgnoreCase);

        private readonly Regex _help =
            new Regex("^!(help)", RegexOptions.IgnoreCase);
        
        private readonly  Regex _tempConv = new Regex("^!temp \\d+(c|f)$", RegexOptions.IgnoreCase);

        private readonly Regex _subreddit = new Regex("(^| |^/| /)r/[^/ ]+", RegexOptions.IgnoreCase);
        private readonly Regex _reddit = new Regex("(com)", RegexOptions.IgnoreCase);

        // Discord config files
        private DiscordSocketClient _client;

        public HaggisBot(bool test)
        {
            // _token =
            //     test ? (string) Prop.GetValue("tokenTest") : (string) Prop.GetValue("token");

            _token = (string) Prop.GetValue("token");

            _gamesChannel = (long) Prop.GetValue("gamesChannel");

            _haggisId = (long) Prop.GetValue("Admin")[0].First.First;

            _roulette = new Roulette(GamePath);

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

            await Task.Delay(-1);
        }

        private async Task MessageReceived(SocketMessage sm)
        {
            if (sm.Author.IsBot)
                return;

            // if ((long) sm.Author.Id == _haggisId)
            //     Console.Out.WriteLine(sm.Content);

            if (sm.Content.ToLower() == "ping")
                await sm.Channel.SendMessageAsync("Pong");
            else if (sm.Content.ToLower() == "pong")
                await sm.Channel.SendMessageAsync("Ping");

            if (_help.IsMatch(sm.Content))
            {
                EmbedBuilder eb = new EmbedBuilder();
                eb.Title = "Help";
                eb.Description = "All commands are case insensitive and are preceded with !";
                eb.Color = Color.Gold;
                eb.AddField("Help", "help");
                eb.AddField("Play Roulette", "rr");
                eb.AddField("Roulette Stats", "rrStats | rrS");
                eb.AddField("Roulette Leaderboard", "rrLB | rrLeaderBoard | rrLead");
                eb.AddField("Roulette Spin", "rrSpin");
                // eb.AddField("Roulette Pistol Whip", "(rrPistolWhip | rrWhip | rrPW) @<user>");
                // eb.AddField("Roulette Counter Whip", "rrCounterWhip | rrCW");
                eb.AddField("Roulette Shoot Player", "(rrSP | rrShootPlayer) @<user>");
                eb.AddField("Ping", "Pong");
                eb.AddField("Convert Temperatures", "temp <Temperature><Unit>");

                _logger.Info("Sending help list: " + sm.Content);
                await sm.Channel.SendMessageAsync(null, false, eb.Build());
            }
            else if (_subreddit.IsMatch(sm.Content) && !_reddit.IsMatch(sm.Content))
            {
                try
                {
                    await sm.Channel.SendMessageAsync(
                        @"https://www.reddit.com/r/" +
                        sm.Content.Split(' ').Single(m => m.Contains(@"r/")).Split(@"r/")[1]);
                }
                catch (Exception e)
                {
                    _logger.Info(e);
                }
            }else if (_tempConv.IsMatch(sm.Content))
            {
                sm.Channel.SendMessageAsync(TemperatureConversion.Convert(sm.Content));
            }

            if ((long) sm.Channel.Id == _gamesChannel)
                try
                {
                    switch (sm.Content)
                    {
                        case var content when _rouletteRegex.IsMatch(content):
                            _logger.Info("Playing round of roulette: " + content);
                            await sm.Channel.SendMessageAsync(_roulette.PlayRound(sm));
                            break;
                        case var content when _rouletteStatsRegex.IsMatch(content):
                            _logger.Info("Getting roulette stats: " + content);
                            var statsReturn = _roulette.GetStats((Int64) sm.Author.Id);
                            await sm.Channel.SendMessageAsync(statsReturn.Item1, false, statsReturn.Item2);
                            break;
                        case var content when _rouletteLeadRegex.IsMatch(content):
                            _logger.Info("Getting roulette leaderboard: " + content);
                            var leadReturn = _roulette.GetLeaders((long) sm.Author.Id);
                            await sm.Channel.SendMessageAsync(leadReturn.Item1, false, leadReturn.Item2);
                            break;
                        case var content when _rouletteSpin.IsMatch(content):
                            _logger.Info("Spinning roulette barrel: " + content);
                            await sm.Channel.SendMessageAsync(_roulette.SpinBarrel());
                            break;
                        // case var content when _rouletteWhip.IsMatch(content):
                        //     _logger.Info("Pistol Whipping Roullete: " + content);
                        //     _roulette.PistolWhip(sm).RunSynchronously();
                        //     break;
                        // case var content when _rouletteWhipCounter.IsMatch(content):
                        //     _logger.Info("Counter Whipping Roulette: " + content);
                        //     await sm.Channel.SendMessageAsync(_roulette.CounterWhip(sm));
                        //     break;
                        case var content when _rouletteShootPlayer.IsMatch(content):
                            _logger.Info("Shooting Player Roulette: " + content);
                            await sm.Channel.SendMessageAsync(_roulette.ShootPlayer(sm));
                            break;
                    }
                }
                catch (Exception e)
                {
                    _logger.Error(e);
                    await sm.Channel.SendMessageAsync(e.Message);
                }
        }
    }
}