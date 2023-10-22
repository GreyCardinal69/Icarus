namespace Icarus.Modules.Other
{
    public static class StringExtensions
    {
        public static string PadBoth( this string str, int length, char ch )
        {
            int spaces = length - str.Length;
            int padLeft = spaces / 2 + str.Length;
            return str.PadLeft( padLeft, ch ).PadRight( length, ch );
        }
    }
}