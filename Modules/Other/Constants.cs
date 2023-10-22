using System;
using System.IO;

namespace Icarus
{
    public static class Constants
    {
        public static string ChannelExportFirstHalf = File.ReadAllText( $"{AppDomain.CurrentDomain.BaseDirectory}Content\\ExportChannelFirstHalf.txt" );
    }
}