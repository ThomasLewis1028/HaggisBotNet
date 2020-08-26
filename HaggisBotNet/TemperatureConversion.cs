using System;
using System.Linq;

namespace HaggisBotNet
{
    public class TemperatureConversion
    {
        public static String Convert(String message)
        {
            var msgArr = message.Split(' ');

            return msgArr.Last().ToLower().EndsWith('f') ? FtoC(msgArr.Last()) : CtoF(msgArr.Last());
        }

        public static String FtoC(String message)
        {
            var tempF = Int32.Parse(message.TrimEnd('f', 'F'));
            var tempC = (tempF - 32) * 5 / 9;
            return tempF + "F is " + tempC + "C";

        }

        public static String CtoF(String message)
        {
            var tempC = Int32.Parse(message.TrimEnd('c', 'C'));
            var tempF = tempC / 5 * 9 + 32;
            return tempC + "C is " + tempF + "F";
        }
    }
}