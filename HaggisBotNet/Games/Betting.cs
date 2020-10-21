﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using HaggisBotNet.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HaggisBotNet.Games
{
    public class Betting
    {
        private readonly string _bettingGamePath;
        private readonly IBetting _bettingGame;

        /// <summary>
        /// Initial constructor.
        /// Receive a path to set up the game on the initial load.
        ///
        /// If no file exists, create a new one.
        /// </summary>
        /// <param name="path"></param>
        public Betting(string path)
        {
            _bettingGamePath = path + "/betting.json";

            if (!File.Exists(_bettingGamePath))
            {
                File.Create(_bettingGamePath).Close();
                _bettingGame = new IBetting
                {
                    Betters = new List<Better>(),
                    Bets = new List<Bet>(),
                    PlayerBets = new List<PlayerBet>()
                };
            }
            else
                _bettingGame = LoadBetting();
        }

        /// <summary>
        /// Receive a SocketMessage and parse out the bet title. Then create a new bet with the author Id/Name and the
        /// bet title.
        /// </summary>
        /// <param name="sm"></param>
        /// <returns></returns>
        public string CreateBet(SocketMessage sm)
        {
            var betTitle = Regex.Split(sm.Content, "^!(createBet|betCreate|cb) ", RegexOptions.IgnoreCase)[2];

            Bet bet = new Bet
            {
                Name = betTitle,
                Id = _bettingGame.Bets.Count + 1,
                BookieId = (Int64) sm.Author.Id,
                BookieName = sm.Author.Username,
                IsActive = true,
                BetPool = 0
            };

            _bettingGame.Bets.Add(bet);

            SerializeData(_bettingGame);
            return $"`{bet.Name}` created by {bet.BookieName} with the bet ID of `{bet.Id}`";
        }

        /// <summary>
        /// Receive a SocketMessage and parse the betId and winningBet from the content.
        ///
        /// Check if the bet exists, and if the person requesting the closure of the bet is the author of the bet.
        /// </summary>
        /// <param name="sm"></param>
        /// <returns></returns>
        public string EndBet(SocketMessage sm)
        {
            var stringSplit = sm.Content.Split(' ');
            var betId = Int32.Parse(stringSplit[1]);
            var winningBet = Int32.Parse(stringSplit[2]);
            var bet = _bettingGame.Bets.Find(b => b.Id == betId);

            if (bet == null)
                return $"Bet `{betId}` does not exist";

            if (!bet.IsActive)
                return $"Bet `{bet.Id}` - `{bet.Name}` has already been closed";

            if (bet.BookieId != (Int64) sm.Author.Id)
                return $"<@{sm.Author.Id}> You are not the bookie for bet `{bet.Id}` - `{bet.Name}`";

            bet.WinningBet = winningBet;
            bet.IsActive = false;

            SerializeData(_bettingGame);
            CalculatePoints(sm, _bettingGame.PlayerBets.FindAll(b => b.BetId == bet.Id), bet);
            return $"Bet `{bet.Id}`- `{bet.Name}` has been closed. \nThe winning bet was {winningBet}";
        }

        /// <summary>
        /// Receive a SocketMessage, a list of PlayerBets, and a Bet.
        ///
        /// Calculate the amount of points that someone earned via betting, either choosing the actual winner,
        /// or those closest to the result.
        /// </summary>
        /// <param name="sm"></param>
        /// <param name="playerBets"></param>
        /// <param name="bet"></param>
        private async void CalculatePoints(SocketMessage sm, List<PlayerBet> playerBets, Bet bet)
        {
            if (playerBets == null)
                await sm.Channel.SendMessageAsync($"No one bet on {bet.Id} - {bet.Name}");

            var playersAndBets = new Dictionary<Int64, Int32>();

            foreach (var playerBet in playerBets)
                playersAndBets.Add(playerBet.BetterId, Math.Abs(bet.WinningBet - playerBet.Bet));

            var sortedPlayerBets =
                from entry
                    in playersAndBets
                orderby entry.Value
                select entry;

            var closestBet = sortedPlayerBets.First().Value;
            var winners = sortedPlayerBets.Where(b => b.Value == closestBet);

            var winningBetters =
                from winner in winners
                join player in _bettingGame.Betters on winner.Key equals player.Id
                select player;

            foreach (var player in winningBetters)
            {
                var value = playersAndBets.First(p => p.Key == player.Id).Value;
                player.Points += value;
                bet.BetPool -= value;
            }
            
            var winnings = bet.BetPool / winners.Count();

            StringBuilder sb = new StringBuilder();
            foreach (var player in winningBetters)
            {
                sb.Append(player.Name + "\n");
                player.Points += winnings;
                player.WonBetsList.Add(bet.Id, bet.Name);
                player.BetsWon++;
            }

            SerializeData(_bettingGame);
        }

        /// <summary>
        /// Receive a SocketMessage and parse it out to determine the betId and bet value requested.
        ///
        /// Search for the related bets and playerBets and add them to the database.
        /// </summary>
        /// <param name="sm"></param>
        /// <returns></returns>
        public string AddBet(SocketMessage sm)
        {
            var stringSplit = sm.Content.Split(' ');
            var betId = Int32.Parse(stringSplit[1]);
            var betValue = Int32.Parse(stringSplit[2]);
            var betPoints = Int32.Parse(stringSplit[3]);
            var bet = _bettingGame.Bets.Find(b => b.Id == betId);
            var betterId = (Int64) sm.Author.Id;
            var betterName = sm.Author.Username;

            if (bet == null)
                return $"Bet `{betId}` does not exist";

            if (!bet.IsActive)
                return $"Bet `{bet.Id}` - `{bet.Name}` has already been closed";

            var better = _bettingGame.Betters.Find(p => p.Id == betterId) ?? new Better
            {
                Id = betterId,
                Name = betterName,
                Points = 1000,
                BetsWon = 0,
                WonBetsList = new Dictionary<int, string>()
            };

            var playerBet = _bettingGame.PlayerBets.Find(b => b.BetId == betId && b.BetterId == better.Id);
            if (playerBet == null)
            {
                if (betPoints > better.Points)
                {
                    return $"<@{sm.Author.Id}> Your bet of {betPoints} was not added to Bet `{bet.Id}` - `{bet.Name}`\n" + 
                        $"You only have {better.Points}";
                }

                if (betPoints < 100)
                {
                    return $"<@{sm.Author.Id}> Your bet of {betPoints} was not added to Bet `{bet.Id}` - `{bet.Name}`\n" + 
                           "Minimum bet is 100";
                }
                
                playerBet = new PlayerBet
                {
                    BetterId = better.Id,
                    BetId = bet.Id,
                    Bet = betValue,
                    Points = betPoints
                };
                
                better.Points -= playerBet.Points;
                _bettingGame.Betters.Add(better);
                bet.BetPool += playerBet.Points;

                _bettingGame.PlayerBets.Add(playerBet);
                SerializeData(_bettingGame);
                return $"<@{sm.Author.Id}> Your bet of {playerBet.Bet} has been added to Bet `{bet.Id}` - `{bet.Name}`";
            }

            return $"<@{sm.Author.Id}> You already have a bet of {playerBet.Bet} on `{bet.Id}` - `{bet.Name}`";
        }

        /// <summary>
        /// Receives a SocketMessage and parses out the betId then searches for that Id and all related items
        /// </summary>
        /// <param name="sm"></param>
        /// <returns></returns>
        public (String, Embed) ViewBet(SocketMessage sm)
        {
            var stringSplit = sm.Content.Split(' ');
            var betId = Int32.Parse(stringSplit[1]);
            var bet = _bettingGame.Bets.Find(b => b.Id == betId);

            if (bet == null)
                return ($"Bet `{betId}` does not exist", null);

            EmbedBuilder eb = new EmbedBuilder();
            eb.WithColor(Color.DarkGreen);
            eb.Title = $"{bet.Id} - {bet.Name}";
            eb.WithAuthor($"Bookie: {bet.BookieName}");
            eb.AddField("Is Active: ", bet.IsActive);
            eb.AddField("Bet Pool: ", bet.BetPool);
            if (!bet.IsActive)
                eb.AddField("Winning bet: ", bet.WinningBet);

            StringBuilder sb = new StringBuilder();
            var playerBets = _bettingGame.PlayerBets.FindAll(b => b.BetId == bet.Id);
            if (playerBets.Count > 0)
            {
                foreach (var playerBet in playerBets)
                {
                    var better = _bettingGame.Betters.Find(p => p.Id == playerBet.BetterId);
                    if (better != null)
                        sb.Append(
                            $"{better.Name,-20} {"-",0} {"Bet: " + playerBet.Bet,15} {"Points: " + playerBet.Points,15}\n");
                }

                eb.AddField("Bets", sb);
            }

            return ("", eb.Build());
        }

        /// <summary>
        /// Receive a SocketMessage and split the content to parse if there was a -all tag.
        ///
        /// Find all active, or all bets in general based on the -all tag's existence.
        /// </summary>
        /// <param name="sm"></param>
        /// <returns></returns>
        public (String, Embed) ListBets(SocketMessage sm)
        {
            var stringSplit = sm.Content.Split(' ');
            bool checkAll = stringSplit.Length > 1 &&
                            new Regex("-all", RegexOptions.IgnoreCase).IsMatch(stringSplit[1]);

            var bets = _bettingGame.Bets.FindAll(p => checkAll ? p.IsActive || !p.IsActive : p.IsActive);

            if (bets.Count == 0)
                return ("There are currently no bets" + (checkAll ? "" : " listed as active"), null);

            EmbedBuilder eb = new EmbedBuilder();
            eb.WithColor(Color.DarkGrey);
            eb.Title = checkAll ? "All bets" : "Active Bets";

            StringBuilder sb = new StringBuilder();

            foreach (var bet in bets)
                sb.Append($"{bet.Id} - {bet.Name} - {bet.BookieName}" +
                          (checkAll
                              ? bet.IsActive
                                  ? " - Active"
                                  : $" - Inactive - Winning Bet: {bet.WinningBet}"
                              : "") + "\n");

            eb.AddField("Bets", sb);

            return ("", eb.Build());
        }

        public (String, Embed) ViewPlayer(SocketMessage sm)
        {
            var stringSplit = sm.Content.Split(' ');
            Int64 playerId;
            if (stringSplit.Length > 1)
                playerId = Int64.Parse(
                    stringSplit[1].Split(new[] {"<@!", ">"}, StringSplitOptions.RemoveEmptyEntries)[1]);
            else
                playerId = (Int64) sm.Author.Id;

            var player = _bettingGame.Betters.Find(b => b.Id == playerId);

            if (player != null)
            {
                EmbedBuilder eb = new EmbedBuilder();
                eb.WithTitle(player.Name);
                eb.WithColor(Color.Purple);
                eb.AddField("Points: ", player.Points);
                eb.AddField("Bets Won: ", player.BetsWon);

                if (player.WonBetsList.Count > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var bet in player.WonBetsList)
                        sb.Append($"{bet.Key} - {bet.Value}\n");

                    eb.AddField("List of Bets Won: ", sb);
                }

                return ("", eb.Build());
            }

            return ("No Player Found", null);
        }

        /// <summary>
        /// Parse a json object and set it to a C# object.
        /// </summary>
        /// <returns></returns>
        private IBetting LoadBetting()
        {
            // Parse the file into a JObject
            var betting =
                JObject.Parse(
                    File.ReadAllText(_bettingGamePath));

            // Deserialize the JObject into a Universe and return it
            return JsonConvert.DeserializeObject<IBetting>(betting.ToString());
        }

        /// <summary>
        /// Receive an IBetting and serialize it to a file.
        /// </summary>
        /// <param name="betting"></param>
        private void SerializeData(IBetting betting)
        {
            // Set the path to the file and write it, overwriting the previous file if it exists.
            var path = _bettingGamePath;
            using var file =
                File.CreateText(path);
            var serializer = new JsonSerializer();
            serializer.Serialize(file, betting);
        }
    }
}