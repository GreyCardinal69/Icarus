using System;
using System.IO;

namespace Icarus
{
    internal class Database
    {
        internal static readonly string[] ScamLinks = File.ReadAllLines( AppDomain.CurrentDomain.BaseDirectory + @"ALL-phishing-links.txt" );
    }
}