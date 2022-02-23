namespace Icarus.Modules.Profiles
{
    public struct AntiSpamProfile
    {
        public int FirstWarning;
        public int SecondWarning;
        public int LastWarning;
        public int Limit;
        public double CacheResetInterval;
    }
}