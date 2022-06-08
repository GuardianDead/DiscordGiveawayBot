using System;

namespace DiscordGivewayBot.Data.Models
{
    public class DiscordRumbleBattle
    {
        public string Path { get; set; }
        public DateTime DateTime { get; set; }

        public DiscordRumbleBattle(string path, DateTime dateTime)
        {
            Path = path;
            DateTime = dateTime;
        }
    }
}
