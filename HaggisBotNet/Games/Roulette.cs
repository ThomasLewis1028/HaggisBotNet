using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using HaggisBotNet.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Games.HaggisBotNet
{
    internal class Roulette
    {
        private static readonly Random Rand = new Random();
        public readonly string GameDataPath;
        public readonly string RouletteGamePath;
        public readonly IRoulette RouletteGame;

        public Roulette(string path)
        {
            GameDataPath = path;
            RouletteGamePath = GameDataPath + "/roulette.json";

            if (!File.Exists(RouletteGamePath))
            {
                File.Create(RouletteGamePath).Close();
                RouletteGame = new IRoulette()
                {
                    Round = Rand.Next(0, 6),
                    Players = new List<Player>(),
                    Played = new List<Int64>()
                };
            }
            else
                RouletteGame = LoadRoulette();
        }

        public String PlayRound(SocketMessage sm)
        {
            string message;
            if (!RouletteGame.Players.Exists(p => p.Id == (Int64) sm.Author.Id))
            {
                RouletteGame.Players.Add(new Player()
                {
                    Id = (Int64) sm.Author.Id,
                    Name = sm.Author.Username,
                    HighestStreak = 0,
                    CurrentStreak = 0,
                    Survives = 0,
                    Deaths = 0,
                    LastPistolWhip = DateTime.UnixEpoch,
                    Whipped = false
                });
            }

            Player player = RouletteGame.Players.Find(p => p.Id == (Int64) sm.Author.Id);

            if (RouletteGame.Played.Contains(player.Id))
            {
                return "<@" + player.Id + "> You've already played this round";
            }

            if (RouletteGame.Round == 0)
            {
                player.Deaths++;
                player.CurrentStreak = 0;
                RouletteGame.Round = Rand.Next(0, 6);
                RouletteGame.Played = new List<Int64>();
                message = "<@" + player.Id +
                          ">\n<:ded:741179781953093652><:bang:741180034626093067><:six_shooter:741153306243760209>";
            }
            else
            {
                player.Survives++;
                player.CurrentStreak++;
                player.HighestStreak = player.CurrentStreak > player.HighestStreak
                    ? player.CurrentStreak
                    : player.HighestStreak;
                RouletteGame.Round--;
                RouletteGame.Played.Add(player.Id);
                message = "<@" + player.Id + ">\n<:stevedamn:392273843034783745><:six_shooter:741153306243760209>";
            }

            RouletteGame.LastPlayed = DateTime.Now;
            SerializeData(RouletteGame);
            return message;
        }

        public async Task PistolWhip(SocketMessage sm)
        {
            var targetId = Int64.Parse(sm.Content.Split(new[] {"<@!", ">"}, StringSplitOptions.RemoveEmptyEntries)[1]);

            if (!RouletteGame.Players.Exists(p => p.Id == (Int64) sm.Author.Id))
            {
                await sm.Channel.SendMessageAsync("<@" + (Int64) sm.Author.Id + "> You haven't played Roulette yet");
                return;
            }

            if (RouletteGame.Played.Contains((Int64) sm.Author.Id))
            {
                await sm.Channel.SendMessageAsync("<@" + (Int64) sm.Author.Id + "> You've already played this round, put the damn gun down");
                return;
            }

            Player player = RouletteGame.Players.Find(p => p.Id == (Int64) sm.Author.Id);

            if (DateTime.Now - player.LastPistolWhip < TimeSpan.FromHours(24))
            {
                await sm.Channel.SendMessageAsync("<@" + (Int64) sm.Author.Id + "> Please wait " + TimeSpan
                                                      .FromHours(24)
                                                      .Subtract(DateTime.Now.Subtract(player.LastPistolWhip))
                                                      .ToString(@"hh\:mm\:ss")
                                                  + " to pistol whip again");
                return;
            }

            if (!RouletteGame.Played.Contains(player.Id) ||
                !RouletteGame.Players.Exists(t => t.Id == targetId))
            {
                player.LastPistolWhip = DateTime.Now;
                await sm.Channel.SendMessageAsync("<@" + (Int64) sm.Author.Id +
                                                  "> You tried pistol whipping someone that hasn't played! How dare you..");
                return;
            }

            Player target =
                RouletteGame.Players.Find(p => p.Id == targetId);

            player.LastPistolWhip = DateTime.Now;
            SerializeData(RouletteGame);
            target.Whipped = true;
            await sm.Channel.SendMessageAsync("<@" + targetId + "> You've been pistol whipped by <@" + player.Id +
                                              ">! You have 30 seconds to respond!");
            Task.Delay(30000).Wait();
            if (target.Whipped)
            {
                target.Whipped = false;
                target.Deaths++;
                RouletteGame.Round = Rand.Next(0, 6);
                RouletteGame.Played = new List<Int64>();
                SerializeData(RouletteGame);
                await sm.Channel.SendMessageAsync("<@" + targetId + "> You were whipped by <@" + player.Id + ">!");
                return;
            }

            target.Survives++;
            RouletteGame.Round = Rand.Next(0, 6);
            RouletteGame.Played = new List<Int64>();
            player.Deaths++;

            SerializeData(RouletteGame);
            await sm.Channel.SendMessageAsync("<@" + player.Id + "> Your whip was countered!");
        }

        public String CounterWhip(SocketMessage sm)
        {
            if (!RouletteGame.Players.Exists(p => p.Id == (Int64) sm.Author.Id))
                return "<@" + (Int64) sm.Author.Id + "> You haven't played Roulette yet";
            if (!RouletteGame.Played.Contains((Int64) sm.Author.Id))
                return "<@" + (Int64) sm.Author.Id + "> You haven't played this round";

            Player player = RouletteGame.Players.Find(p => p.Id == (Int64) sm.Author.Id);

            if (!player.Whipped)
                return "<@" + player.Id + "> You haven't been whipped";

            player.Whipped = false;

            SerializeData(RouletteGame);
            return "<@" + player.Id + "> You countered the whip!";
        }

        public (String, Embed) GetStats(Int64 id)
        {
            if (!RouletteGame.Players.Exists(p => p.Id == id))
                return ("<@" + id + "> You haven't played yet", null);

            Player player = RouletteGame.Players.Find(p => p.Id == id);

            EmbedBuilder eb = new EmbedBuilder();
            eb.WithColor(Color.Blue);
            eb.Title = "Russian Roulette Stats";

            eb.AddField("Current Streak: ", player.CurrentStreak);
            eb.AddField("Highest Streak: ", player.HighestStreak);
            eb.AddField("Survives: ", player.Survives);
            eb.AddField("Deaths", player.Deaths);
            eb.AddField("K/D: ", player.KillDeath);

            return ("<@" + id + "> Here are your stats", eb.Build());
        }

        public (String, Embed) GetLeaders(Int64 id)
        {
            if (RouletteGame.Players.Count == 0)
            {
                return ("<@" + id + "> No one has played yet", null);
            }

            EmbedBuilder eb = new EmbedBuilder();
            eb.WithColor(Color.Blue);
            eb.Title = "Russian Roulette Leaderboard";

            eb.AddField("Highest Current Streak: ",
                RouletteGame.Players.Find(p => p.Id == RouletteGame.HighestCurrent)?.Name + " - " +
                RouletteGame.Players.Find(p => p.Id == RouletteGame.HighestCurrent)?.CurrentStreak);
            eb.AddField("Highest Streak Ever: ",
                RouletteGame.Players.Find(p => p.Id == RouletteGame.HighestTop)?.Name + " - " +
                RouletteGame.Players.Find(p => p.Id == RouletteGame.HighestTop)?.HighestStreak);
            eb.AddField("Most Survives: ",
                RouletteGame.Players.Find(p => p.Id == RouletteGame.HighestSurvives)?.Name + " - " +
                RouletteGame.Players.Find(p => p.Id == RouletteGame.HighestSurvives)?.Survives);
            eb.AddField("Most Deaths: ",
                RouletteGame.Players.Find(p => p.Id == RouletteGame.HighestDeaths)?.Name + " - " +
                RouletteGame.Players.Find(p => p.Id == RouletteGame.HighestDeaths)?.Deaths);
            eb.AddField("Highest K/D: ",
                RouletteGame.Players.Find(p => p.Id == RouletteGame.HighestKD)?.Name + " - " +
                RouletteGame.Players.Find(p => p.Id == RouletteGame.HighestKD)?.KillDeath);


            return ("<@" + id + "> Here is the leaderboard", eb.Build());
        }

        public String SpinBarrel()
        {
            if (DateTime.Now - RouletteGame.LastPlayed >= TimeSpan.FromMinutes(5))
            {
                RouletteGame.LastPlayed = DateTime.Now;
                RouletteGame.Played = new List<Int64>();
                RouletteGame.Round = Rand.Next(0, 6);
                return "*SPIN* <:six_shooter:741153306243760209>";
            }

            var waitTime = TimeSpan.FromMinutes(5).Subtract(DateTime.Now.Subtract(RouletteGame.LastPlayed));

            return "Please wait " + waitTime.ToString("m\\:ss");
        }
 
        public IRoulette LoadRoulette()
        {
            // Parse the file into a JObject
            var roulette =
                JObject.Parse(
                    File.ReadAllText(RouletteGamePath));

            // Deserialize the JObject into a Universe and return it
            return JsonConvert.DeserializeObject<IRoulette>(roulette.ToString());
        }

        private void SerializeData(IRoulette roulette)
        {
            // Set the path to the file and write it, overwriting the previous file if it exists.
            var path = RouletteGamePath;
            using var file =
                File.CreateText(path);
            var serializer = new JsonSerializer();
            serializer.Serialize(file, roulette);
        }

        private static T LoadData<T>(String path)
        {
            var data =
                JObject.Parse(
                    File.ReadAllText(path));

            return JsonConvert.DeserializeObject<T>(data.ToString());
        }
    }
}