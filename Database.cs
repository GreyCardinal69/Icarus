using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus
{
    public class Database
    {
        public static readonly string[] ScamLinks = File.ReadAllLines( AppDomain.CurrentDomain.BaseDirectory + @"ALL-phishing-links.txt" );
    }
}