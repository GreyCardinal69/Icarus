using System;

namespace Icarus.Modules.Profiles
{
    public class TimedReminder
    {
        public bool HasExpired( DateTimeOffset now )
        {
            return now.ToUnixTimeSeconds() >= ExpDate;
        }

        public void UpdateExpDate()
        {
            string[] times;
            DateTimeOffset temp;
            DateTimeOffset current = DateTimeOffset.UtcNow;

            switch ( DateFormat )
            {
                case "-r":
                    // 4d, 4d 2h etc
                    times = Date.Split( '-' );
                    ExpDate = DateTimeOffset.UtcNow.AddDays( Convert.ToDouble( times[0] ) ).AddHours( Convert.ToDouble( times[1] ) ).AddMinutes( Convert.ToDouble( times[2] ) ).ToUnixTimeSeconds();
                    break;
                case "-t":
                    // saturday x hour, so on
                    times = Date.Split( '-' );

                    temp = new DateTimeOffset( current.Year, current.Month, current.Day, Math.Max( 0, Convert.ToInt32( times[1] ) - 1 ), 0, 0, new TimeSpan() );
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
                    break;
                case "-e":

                    // specific day of specific month
                    times = Date.Split( '-' );

                    int month = Convert.ToInt32( times[0] );
                    int day = Convert.ToInt32( times[1] );
                    int hour = Convert.ToInt32( times[2] );

                    temp = new DateTimeOffset( current.Year, month, day, Math.Max( 0, hour - 1 ), 0, 0, new TimeSpan() );
                    ExpDate = temp.ToUnixTimeSeconds();
                    break;
            }
        }

        public string ToString()
        {
            return $"Timed Reminder: `{Name}` \nContent: {Content} \n`{DateTime.Now}`";
        }

        public TimedReminder( string name, string content, bool repeat, string dateFormat, string date, long expdate = 0 )
        {
            Name = name;
            Content = content;
            Repeat = repeat;

            if ( dateFormat == "-e" )
            {
                Repeat = false;
            }

            Date = date;
            DateFormat = dateFormat;
            StartDate = DateTime.UtcNow;

            if ( expdate != 0 )
            {
                ExpDate = expdate;
                return;
            }

            UpdateExpDate( );
        }

        public string Name { get; private set; }
        public bool Repeat { get; private set; }
        public long ExpDate;
        public DateTime StartDate { get; private set; }
        public string Content { get; private set; }
        public string Date { get; private set; }
        public string DateFormat { get; private set; }
    }
}