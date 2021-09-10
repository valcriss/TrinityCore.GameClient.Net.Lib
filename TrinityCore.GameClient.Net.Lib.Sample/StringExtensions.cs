using System;
using System.Collections.Generic;
using System.Text;

namespace TrinityCore.GameClient.Net.Lib.Sample
{
    public static class StringExtensions
    {
        public static string EnsureLength(this string value, int minLength)
        {
            while (value.Length < minLength)
                value += " ";
            return value;
        }
    }
}
