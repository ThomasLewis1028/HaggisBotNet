using System;
using System.Collections.Generic;
using System.IO;
using Discord.WebSocket;
using HaggisBotNet.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Games.HaggisBotNet
{
    public class Betting
    {
        private static readonly Random Rand = new Random();
        public readonly string GameDataPath;
        public readonly string BettingGamePath;
        public readonly IBetting BettingGame;

        /// <summary>
        /// Initial constructor.
        /// Receive a path to set up the game on the initial load.
        ///
        /// If no file exists, create a new one.
        /// </summary>
        /// <param name="path"></param>
        public Betting(string path)
        {
            GameDataPath = path;
            BettingGamePath = GameDataPath + "/betting.json";

            if (!File.Exists(BettingGamePath))
            {
                File.Create(BettingGamePath).Close();
                BettingGame = new IBetting()
                {
                    Betters = new List<Better>(),
                    Bets = new List<Bet>()
                };
            }
            else
                BettingGame = LoadBetting();
        }

        public string CreateBet(SocketMessage sm)
        {
            
            
            return "Bet created";
        }
        
        /// <summary>
        /// Parse a json object and set it to a C# object.
        /// </summary>
        /// <returns></returns>
        public IBetting LoadBetting()
        {
            // Parse the file into a JObject
            var roulette =
                JObject.Parse(
                    File.ReadAllText(BettingGamePath));

            // Deserialize the JObject into a Universe and return it
            return JsonConvert.DeserializeObject<IBetting>(roulette.ToString());
        }
    }
}