namespace colosseumbackend.Common;

public static class RateLimitConfig
{
    public static class Policies
    {
        public const string Auth  = "Auth";
        public const string Read  = "Read";
        public const string Write = "Write";
    }

    public static class Limits
    {
        public const int AuthPerMinute   = 20;
        public const int ReadPerMinute   = 300;
        public const int WritePerMinute  = 60;
        public const int GlobalPerMinute = 600;
    }
}
