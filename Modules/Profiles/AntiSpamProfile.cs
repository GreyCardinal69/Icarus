namespace Icarus.Modules.Profiles
{
    public struct AntiSpamProfile
    {
        public AntiSpamProfile()
        {
            FirstWarning = 9;
            SecondWarning = 15;
            LastWarning = 19;
            Limit = 24;
        }

        public int FirstWarning;
        public int SecondWarning ;
        public int LastWarning;
        public int Limit ;
        public double CacheResetInterval;
    }
}