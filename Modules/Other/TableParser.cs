﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Icarus.Modules.Other
{
    public class TableParser
    {
        private readonly string[] titles;
        private readonly List<int> lengths;
        private readonly List<string[]> rows = new List<string[]>();
        const string TOP_LEFT_JOINT = "┌";
        const string TOP_RIGHT_JOINT = "┐";
        const string BOTTOM_LEFT_JOINT = "└";
        const string BOTTOM_RIGHT_JOINT = "┘";
        const string TOP_JOINT = "┬";
        const string BOTTOM_JOINT = "┴";
        const string LEFT_JOINT = "├";
        const string JOINT = "┼";
        const string RIGHT_JOINT = "┤";
        const char HORIZONTAL_LINE = '─';
        const char PADDING = ' ';
        const string VERTICAL_LINE = "│";

        public TableParser( params string[] titles )
        {
            this.titles = titles;
            lengths = titles.Select( t => t.Length ).ToList();
        }

        public void AddRow( params object[] row )
        {
            if ( row.Length != titles.Length )
            {
                throw new System.Exception( $"Added row length [{row.Length}] is not equal to title row length [{titles.Length}]" );
            }
            rows.Add( row.Select( o => o.ToString() ).ToArray() );
            for ( int i = 0; i < titles.Length; i++ )
            {
                if ( rows.Last()[i].Length > lengths[i] )
                {
                    lengths[i] = rows.Last()[i].Length;
                }
            }
        }

        public string Print()
        {
            return GetDataInTableFormat( rows );
        }

        private int[] GetMaxCellWidths( List<string[]> table )
        {
            int maximumCells = 0;
            foreach ( Array row in table )
            {
                if ( row.Length > maximumCells )
                    maximumCells = row.Length;
            }

            int[] maximumCellWidths = new int[maximumCells];
            for ( int i = 0; i < maximumCellWidths.Length; i++ )
                maximumCellWidths[i] = 0;

            foreach ( Array row in table )
            {
                for ( int i = 0; i < row.Length; i++ )
                {
                    if ( row.GetValue( i ).ToString().Length > maximumCellWidths[i] )
                        maximumCellWidths[i] = row.GetValue( i ).ToString().Length;
                }
            }

            return maximumCellWidths;
        }

        public string GetDataInTableFormat( List<string[]> table )
        {
            StringBuilder formattedTable = new StringBuilder();
            Array nextRow = table.FirstOrDefault();
            Array previousRow = table.FirstOrDefault();

            if ( table == null || nextRow == null )
                return String.Empty;

            // FIRST LINE:
            int[] maximumCellWidths = GetMaxCellWidths( table );
            for ( int i = 0; i < nextRow.Length; i++ )
            {
                if ( i == 0 && i == nextRow.Length - 1 )
                    formattedTable.Append( String.Format( "{0}{1}{2}", TOP_LEFT_JOINT, String.Empty.PadLeft( maximumCellWidths[i], HORIZONTAL_LINE ), TOP_RIGHT_JOINT ) );
                else if ( i == 0 )
                    formattedTable.Append( String.Format( "{0}{1}", TOP_LEFT_JOINT, String.Empty.PadLeft( maximumCellWidths[i], HORIZONTAL_LINE ) ) );
                else if ( i == nextRow.Length - 1 )
                    formattedTable.AppendLine( String.Format( "{0}{1}{2}", TOP_JOINT, String.Empty.PadLeft( maximumCellWidths[i], HORIZONTAL_LINE ), TOP_RIGHT_JOINT ) );
                else
                    formattedTable.Append( String.Format( "{0}{1}", TOP_JOINT, String.Empty.PadLeft( maximumCellWidths[i], HORIZONTAL_LINE ) ) );
            }
            bool headerPadded = false;
            int rowIndex = 0;
            int lastRowIndex = table.Count - 1;
            foreach ( Array thisRow in table )
            {
                // LINE WITH VALUES:
                int cellIndex = 0;
                int lastCellIndex = thisRow.Length - 1;
                foreach ( object thisCell in thisRow )
                {
                    string thisValue;
                    if ( cellIndex <= lastCellIndex && !headerPadded )
                    {
                        thisValue = thisCell.ToString().PadBoth( maximumCellWidths[cellIndex], PADDING );
                        if ( cellIndex == lastCellIndex ) headerPadded = true;
                    }
                    else
                    {
                        thisValue = thisCell.ToString().PadRight( maximumCellWidths[cellIndex], PADDING );
                    }

                    if ( cellIndex == 0 && cellIndex == lastCellIndex )
                        formattedTable.AppendLine( String.Format( "{0}{1}{2}", VERTICAL_LINE, thisValue, VERTICAL_LINE ) );
                    else if ( cellIndex == 0 )
                        formattedTable.Append( String.Format( "{0}{1}", VERTICAL_LINE, thisValue ) );
                    else if ( cellIndex == lastCellIndex )
                        formattedTable.AppendLine( String.Format( "{0}{1}{2}", VERTICAL_LINE, thisValue, VERTICAL_LINE ) );
                    else
                        formattedTable.Append( String.Format( "{0}{1}", VERTICAL_LINE, thisValue ) );

                    cellIndex++;
                }

                previousRow = thisRow;

                // SEPARATING LINE:
                if ( rowIndex != lastRowIndex )
                {
                    nextRow = table[rowIndex + 1];

                    int maximumCells = Math.Max( previousRow.Length, nextRow.Length );
                    for ( int i = 0; i < maximumCells; i++ )
                    {
                        if ( i == 0 && i == maximumCells - 1 )
                        {
                            formattedTable.Append( String.Format( "{0}{1}{2}", LEFT_JOINT, String.Empty.PadLeft( maximumCellWidths[i], HORIZONTAL_LINE ), RIGHT_JOINT ) );
                        }
                        else if ( i == 0 )
                        {
                            formattedTable.Append( String.Format( "{0}{1}", LEFT_JOINT, String.Empty.PadLeft( maximumCellWidths[i], HORIZONTAL_LINE ) ) );
                        }
                        else if ( i == maximumCells - 1 )
                        {
                            if ( i > previousRow.Length )
                                formattedTable.AppendLine( String.Format( "{0}{1}{2}", TOP_JOINT, String.Empty.PadLeft( maximumCellWidths[i], HORIZONTAL_LINE ), TOP_RIGHT_JOINT ) );
                            else if ( i > nextRow.Length )
                                formattedTable.AppendLine( String.Format( "{0}{1}{2}", BOTTOM_JOINT, String.Empty.PadLeft( maximumCellWidths[i], HORIZONTAL_LINE ), BOTTOM_RIGHT_JOINT ) );
                            else if ( i > previousRow.Length - 1 )
                                formattedTable.AppendLine( String.Format( "{0}{1}{2}", JOINT, String.Empty.PadLeft( maximumCellWidths[i], HORIZONTAL_LINE ), TOP_RIGHT_JOINT ) );
                            else if ( i > nextRow.Length - 1 )
                                formattedTable.AppendLine( String.Format( "{0}{1}{2}", JOINT, String.Empty.PadLeft( maximumCellWidths[i], HORIZONTAL_LINE ), BOTTOM_RIGHT_JOINT ) );
                            else
                                formattedTable.AppendLine( String.Format( "{0}{1}{2}", JOINT, String.Empty.PadLeft( maximumCellWidths[i], HORIZONTAL_LINE ), RIGHT_JOINT ) );
                        }
                        else
                        {
                            if ( i > previousRow.Length )
                                formattedTable.Append( String.Format( "{0}{1}", TOP_JOINT, String.Empty.PadLeft( maximumCellWidths[i], HORIZONTAL_LINE ) ) );
                            else if ( i > nextRow.Length )
                                formattedTable.Append( String.Format( "{0}{1}", BOTTOM_JOINT, String.Empty.PadLeft( maximumCellWidths[i], HORIZONTAL_LINE ) ) );
                            else
                                formattedTable.Append( String.Format( "{0}{1}", JOINT, String.Empty.PadLeft( maximumCellWidths[i], HORIZONTAL_LINE ) ) );
                        }
                    }
                }

                rowIndex++;
            }

            // LAST LINE:
            for ( int i = 0; i < previousRow.Length; i++ )
            {
                if ( i == 0 )
                    formattedTable.Append( String.Format( "{0}{1}", BOTTOM_LEFT_JOINT, String.Empty.PadLeft( maximumCellWidths[i], HORIZONTAL_LINE ) ) );
                else if ( i == previousRow.Length - 1 )
                    formattedTable.AppendLine( String.Format( "{0}{1}{2}", BOTTOM_JOINT, String.Empty.PadLeft( maximumCellWidths[i], HORIZONTAL_LINE ), BOTTOM_RIGHT_JOINT ) );
                else
                    formattedTable.Append( String.Format( "{0}{1}", BOTTOM_JOINT, String.Empty.PadLeft( maximumCellWidths[i], HORIZONTAL_LINE ) ) );
            }

            return formattedTable.ToString();
        }
    }
}