using System;

namespace HaggisBotNet
{
    public class Program
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static void Main(string[] args)
        {
            try
            {
                var bot = new HaggisBot(args.Length > 0 && args[0] == "-test");
                bot?.MainAsync();


                while (true)
                {
                }
            }
            catch (Exception e)
            {
                Logger.Info(e);
            }
        }
    }
}