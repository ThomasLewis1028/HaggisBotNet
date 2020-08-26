using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace HaggisBotNet
{
    public class TemperatureConversion
    {
        public static String Convert(String message)
        {
            var msgArr = Regex.Split(message, "( |f|c)", RegexOptions.IgnoreCase);
            var temp = Double.Parse(msgArr[2]);
            var unit = msgArr[3].ToLower();

            return unit == "f"
                ? String.Format("{0}F is {1}C",  temp, Math.Round((temp - 32) * (5.0 / 9.0), 2))
                : String.Format("{0}C is {1}F",  temp, Math.Round(temp * 1.8 + 32, 2));
        }
    }
}