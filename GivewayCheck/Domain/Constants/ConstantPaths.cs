using System;

namespace DiscordGivewayBot.Domain.Constants
{
    static public class ConstantPaths
    {
        static public string launchConfigurationPath = $@"{Environment.CurrentDirectory}/launchConfiguration.json";
        static public string logPath = $@"{Environment.CurrentDirectory}/logs.txt";
        static public string participateGiveawayPath = $@"{Environment.CurrentDirectory}/participateGiveawaysPath.json";
        static public string endedGiveawayPath = $@"{Environment.CurrentDirectory}/endedGiveawaysPath.json";
    }
}
