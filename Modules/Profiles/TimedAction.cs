using System;

namespace Icarus.Modules.Profiles
{
    public class TimedReminder
    {
        public bool HasExpired( DateTimeOffset now )
        {
            var r = DateTimeOffset.FromUnixTimeSeconds( ExpDate );
            return now.ToUnixTimeSeconds() >= ExpDate;
        }

        public void UpdateExpDate( DateTimeOffset now )
        {
            string[] times;
            // 4d, 4d 2h etc
            if ( !IsSpecific )
            {
                times = Date.Split( '-' );
                ExpDate = DateTimeOffset.UtcNow.AddDays( Convert.ToDouble( times[0] ) ).AddHours( Convert.ToDouble( times[1] ) ).AddMinutes( Convert.ToDouble( times[2] ) ).ToUnixTimeSeconds();
                return;
            }

            // saturday x hour, so on

            times = Date.Split( '-' );
            IsSpecific = true;
            DateTime current = DateTime.UtcNow;

            DateTimeOffset temp = new DateTimeOffset( current.Year, current.Month, current.Day, Math.Max(0,Convert.ToInt32( times[1] ) - 1), 0, 0, new TimeSpan() );
            int num = 0;

            switch ( times[0].ToLower() )
            {
                case "mo":
                    num = ( int ) DayOfWeek.Monday;
                    break;
                case "tu":
                    num = ( int ) DayOfWeek.Tuesday;
                    break;
                case "we":
                    num = ( int ) DayOfWeek.Wednesday;
                    break;
                case "th":
                    num = ( int ) DayOfWeek.Thursday;
                    break;
                case "fr":
                    num = ( int ) DayOfWeek.Friday;
                    break;
                case "sa":
                    num = ( int ) DayOfWeek.Saturday;
                    break;
                case "su":
                    num = ( int ) DayOfWeek.Sunday;
                    break;
            }

            temp = temp.AddDays( num - ( int ) temp.DayOfWeek );

            if ( DateTimeOffset.UtcNow >= temp )
            {
                temp = temp.AddDays( 7 );
            }

            ExpDate = temp.ToUnixTimeSeconds();
        }

        public string ToString()
        {
            return $"Timed Reminder: `{Name}` \nContent: {Content}.\n`{DateTime.Now}`.";
        }

        public TimedReminder( string name, string content, bool repeat, string dateType, string date )
        {
            Name = name;
            Content = content;
            Repeat = repeat;
            Date = date;

            if ( dateType != "-r" )
            {
                IsSpecific = true;
            }

            StartDate = DateTime.UtcNow;
            UpdateExpDate( StartDate );
        }

        public string Name { get; private set; }
        public bool Repeat { get; private set; }
        public bool IsSpecific { get; private set; }
        public long ExpDate { get; private set; }
        public DateTime StartDate { get; private set; }
        public string Content { get; private set; }
        public string Date { get; private set; }
    }
}