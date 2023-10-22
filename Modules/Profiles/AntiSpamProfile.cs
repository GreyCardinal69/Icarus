namespace Icarus.Modules.Profiles
{
    public struct AntiSpamProfile
    {
        public AntiSpamProfile()
        {
            FirstWarning = 1000;
            SecondWarning = 1000;
            LastWarning = 1000;
            Limit = 1000;
        }

        public int FirstWarning;
        public int SecondWarning ;
        public int LastWarning;
        public int Limit ;
        public double CacheResetInterval;
    }
}