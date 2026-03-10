using System;
using System.Text.RegularExpressions;

namespace TrinityCore.GameClient.Net.Navigation.MmapEngine.Tools
{
    public static class StringExtensions
    {
        #region Public Methods

        public static bool CheckExtension(this string file, string expectedExtention)
        {
            try
            {
                string ext = System.IO.Path.GetExtension(file).ToLower();
                return (expectedExtention.StartsWith(".") ? expectedExtention.ToLower() : "." + expectedExtention.ToLower()) == ext;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        public static bool CheckRegExp(this string file, string regexp)
        {
            try
            {
                string filename = System.IO.Path.GetFileNameWithoutExtension(file);
                return Regex.Match(filename, regexp).Success;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        public static bool Exists(this string file)
        {
            return System.IO.File.Exists(file);
        }

        #endregion Public Methods
    }
}

