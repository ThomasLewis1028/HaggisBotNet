using System;
using System.IO;
using HaggisBotNet.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Games.HaggisBotNet
{
    public class Games
    {
        public readonly string _gameDataPath;
        public static IRoulette RouletteData;
        
        public Games(string path)
        {
            _gameDataPath = path;
            // RouletteData = LoadData<IRoulette>(@"/GameData/")
        }
        
        // public IRoulette CreateGame(string path)
        // {
        //     
        // }
        
        /// <summary>
        /// Receive the path to a data type and return the deserialized version of that data
        /// </summary>
        /// <param name="path"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static T LoadData<T>(String path)
        {
            var data =
                JObject.Parse(
                    File.ReadAllText(path));

            return JsonConvert.DeserializeObject<T>(data.ToString());
        }
    }
}