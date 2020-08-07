namespace HaggisBotNet
{
    public class Program
    {
        private static void Main(string[] args)
        {
            var bot = new HaggisBot(args[0] == "-test");
            bot?.MainAsync();

            while (true)
            {
            }
        }
    }
}