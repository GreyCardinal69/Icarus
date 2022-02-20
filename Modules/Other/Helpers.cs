using System;
using System.Collections.Generic;

namespace Icarus.Modules.Other
{
    public sealed class Helpers
    {
        public static int LevenshteinDistance ( string s, string t )
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];
            if (n == 0)
            {
                return m;
            }
            if (m == 0)
            {
                return n;
            }
            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = ( t[j - 1] == s[i - 1] ) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min( d[i - 1, j] + 1, d[i, j - 1] + 1 ),
                        d[i - 1, j - 1] + cost );
                }
            }
            return d[n, m];
        }

        public static List<string> GetAllFilesFromFolder ( string root, bool searchSubfolders )
        {
            Queue<string> folders = new();
            List<string> files = new();
            folders.Enqueue( root );
            while (folders.Count != 0)
            {
                string currentFolder = folders.Dequeue();
                try
                {
                    string[] filesInCurrent = System.IO.Directory.GetFiles( currentFolder, "*.*", System.IO.SearchOption.TopDirectoryOnly );
                    files.AddRange( filesInCurrent );
                }
                catch { }
                try
                {
                    if (searchSubfolders)
                    {
                        string[] foldersInCurrent = System.IO.Directory.GetDirectories( currentFolder, "*.*", System.IO.SearchOption.TopDirectoryOnly );
                        foreach (string _current in foldersInCurrent)
                        {
                            folders.Enqueue( _current );
                        }
                    }
                }
                catch { }
            }
            return files;
        }
    }
}