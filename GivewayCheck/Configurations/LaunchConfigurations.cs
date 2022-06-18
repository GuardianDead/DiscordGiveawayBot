using GivewayCheck.Services;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace DiscordGivewayBot.Configurations
{
    public class LaunchConfigurations
    {
        static private string launchConfigurationPath = $@"{Environment.CurrentDirectory}/LaunchConfigurations.json";

        static public string LogPath { get => $@"{Environment.CurrentDirectory}/Logs.txt"; }
        static public string ParticipateGiveawaysPath { get => $@"{Environment.CurrentDirectory}/ParticipateGiveaways.txt"; }
        static public string ParticipateRumbleBattlesPath { get => $@"{Environment.CurrentDirectory}/ParticipateRumbleBattles.txt"; }
        static public string WonGiveawaysPath { get => $@"{Environment.CurrentDirectory}/WonGiveaways.txt"; }
        static public string WonRumbleBattlesPath { get => $@"{Environment.CurrentDirectory}/WonRumbleBattles.txt"; }
        static public string WonWLsPath { get => $@"{Environment.CurrentDirectory}/WonWL's.txt"; }
        public string BadGiveawayPhrasesPath { get; private init; }
        public string DiscordGiveawayBotsPath { get; private init; }
        public string DiscordAccountsPath { get; private init; }

        public int DelayBeforeDiscordAccountsWlRoleChecking { get; private init; }
        public int MinDelayBeforeDiscordGiveawayBotsChecking { get; private init; }
        public int MaxDelayBeforeDiscordGiveawayBotsChecking { get; private init; }
        public int MinDelayBeforeFoundGiveaway { get; private init; }
        public int MaxDelayBeforeFoundGiveaway { get; private init; }
        public int MinDelayEachDiscordAccountReact { get; private init; }
        public int MaxDelayEachDiscordAccountReact { get; private init; }

        public LaunchConfigurations()
        {
            LogService.LogMessage("Reading the configuration file..");
            var launchConfiguration = JObject.Parse(File.ReadAllText(launchConfigurationPath));

            DiscordGiveawayBotsPath = launchConfiguration["discordGiveawayBotsPath"].ToString();
            DiscordAccountsPath = launchConfiguration["discordAccountsPath"].ToString();
            BadGiveawayPhrasesPath = launchConfiguration["badGiveawayPhrasesPath"].ToString();

            DelayBeforeDiscordAccountsWlRoleChecking = int.Parse(launchConfiguration["delayBeforeDiscordAccountsWlRoleChecking"].ToString());
            var delaysBeforeDiscordGiveawayBotsChecking = launchConfiguration["delayAfterDiscordGiveawayBotsChecking"].ToString().Split('-');
            var delaysBeforeFoundGiveaway = launchConfiguration["delayBeforeFoundGiveaway"].ToString().Split('-');
            var delaysEachDiscordAccountReact = launchConfiguration["delayEachDiscordAccountReact"].ToString().Split('-');

            switch (delaysBeforeDiscordGiveawayBotsChecking.Length)
            {
                case 1:
                    MinDelayBeforeDiscordGiveawayBotsChecking = int.Parse(delaysBeforeDiscordGiveawayBotsChecking[0]);
                    MaxDelayBeforeDiscordGiveawayBotsChecking = int.Parse(delaysBeforeDiscordGiveawayBotsChecking[0]);
                    break;
                case 2:
                    MinDelayBeforeDiscordGiveawayBotsChecking = int.Parse(delaysBeforeDiscordGiveawayBotsChecking[0]);
                    MaxDelayBeforeDiscordGiveawayBotsChecking = int.Parse(delaysBeforeDiscordGiveawayBotsChecking[1]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(delaysBeforeDiscordGiveawayBotsChecking), "Delay before checking all discord giveaway bots entered incorrectly");
            }
            switch (delaysBeforeFoundGiveaway.Length)
            {
                case 1:
                    MinDelayBeforeFoundGiveaway = int.Parse(delaysBeforeFoundGiveaway[0]);
                    MaxDelayBeforeFoundGiveaway = int.Parse(delaysBeforeFoundGiveaway[0]);
                    break;
                case 2:
                    MinDelayBeforeFoundGiveaway = int.Parse(delaysBeforeFoundGiveaway[0]);
                    MaxDelayBeforeFoundGiveaway = int.Parse(delaysBeforeFoundGiveaway[1]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(delaysBeforeFoundGiveaway), "The delay after the found giveaway is entered incorrectly");
            }
            switch (delaysEachDiscordAccountReact.Length)
            {
                case 1:
                    MinDelayEachDiscordAccountReact = int.Parse(delaysEachDiscordAccountReact[0]);
                    MaxDelayEachDiscordAccountReact = int.Parse(delaysEachDiscordAccountReact[0]);
                    break;
                case 2:
                    MinDelayEachDiscordAccountReact = int.Parse(delaysEachDiscordAccountReact[0]);
                    MaxDelayEachDiscordAccountReact = int.Parse(delaysEachDiscordAccountReact[1]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(MinDelayEachDiscordAccountReact), "The delay after putting down the reaction of each discord account is entered incorrectly");
            }
        }
    }
}
