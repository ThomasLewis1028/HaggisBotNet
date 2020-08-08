using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Games.HaggisBotNet;
using Newtonsoft.Json.Linq;

namespace HaggisBotNet
{
    internal class HaggisBot
    {
        // private static Games.HaggisBotNet.Games _games;
        private static Roulette _roulette;

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
            new Regex("^!(rrStats)($| .*)", RegexOptions.IgnoreCase);

        private readonly Regex _rouletteLeadRegex =
            new Regex("^!(rrLB|rrLeaderBoard|rrLead)($| .*)", RegexOptions.IgnoreCase);

        private readonly Regex _rouletteSpin =
            new Regex("^!(rrSpin)($| .*)", RegexOptions.IgnoreCase);

        private readonly Regex _rouletteWhip =
            new Regex("^!(rrPistolWhip|rrWhip|rrPW) <@!(\\d+)>($| .*)", RegexOptions.IgnoreCase);
        
        private readonly Regex _rouletteWhipCounter =
            new Regex("^!(rrCounterWhip|rrCW)", RegexOptions.IgnoreCase);

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
            await _client.SetGameAsync("No Universe Loaded");

            await Console.Out.WriteLineAsync("DiscordBot Connected");

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

            if ((long) sm.Channel.Id == _gamesChannel)
                switch (sm.Content)
                {
                    case var content when _rouletteRegex.IsMatch(content):
                        await sm.Channel.SendMessageAsync(_roulette.PlayRound(sm));
                        break;
                    case var content when _rouletteStatsRegex.IsMatch(content):
                        var statsReturn = _roulette.GetStats((Int64) sm.Author.Id);
                        await sm.Channel.SendMessageAsync(statsReturn.Item1, false, statsReturn.Item2);
                        break;
                    case var content when _rouletteLeadRegex.IsMatch(content):
                        var leadReturn = _roulette.GetLeaders((long) sm.Author.Id);
                        await sm.Channel.SendMessageAsync(leadReturn.Item1, false, leadReturn.Item2);
                        break;
                    case var content when _rouletteSpin.IsMatch(content):
                        await sm.Channel.SendMessageAsync(_roulette.SpinBarrel());
                        break;
                    case var content when _rouletteWhip.IsMatch(content):
                        _roulette.PistolWhip(sm).RunSynchronously();
                        break;
                    case var content when _rouletteWhipCounter.IsMatch(content):
                        await sm.Channel.SendMessageAsync(_roulette.CounterWhip(sm));
                        break;
                }
        }
    }
}