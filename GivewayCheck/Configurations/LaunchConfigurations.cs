using GivewayCheck.Services;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace DiscordGivewayBot.Configurations
{
    public class LaunchConfigurations
    {
        private string launchConfigurationPath = $@"{Environment.CurrentDirectory}/LaunchConfigurations.json";

        public string LogPath { get => $@"{Environment.CurrentDirectory}/Logs.txt"; }
        public string ParticipateGiveawayPath { get => $@"{Environment.CurrentDirectory}/ParticipateGiveawaysPath.json"; }
        public string EndedGiveawayPath { get => $@"{Environment.CurrentDirectory}/EndedGiveawaysPath.json"; }
        public string BadGiveawayPhrasesPath { get; private init; }
        public string DiscordGuildsPath { get; private init; }
        public string DiscordAccountsPath { get; private init; }

        public int MinDelayBeforeDiscordGuildsChecking { get; private init; }
        public int MaxDelayBeforeDiscordGuildsChecking { get; private init; }
        public int MinDelayBeforeFoundGiveaway { get; private init; }
        public int MaxDelayBeforeFoundGiveaway { get; private init; }
        public int MinDelayEachDiscordAccountReact { get; private init; }
        public int MaxDelayEachDiscordAccountReact { get; private init; }

        public LaunchConfigurations()
        {
            LogService.LogMessage("Reading the configuration file..");
            var launchConfiguration = JObject.Parse(File.ReadAllText(launchConfigurationPath));

            DiscordGuildsPath = launchConfiguration["discordGuildsPath"].ToString();
            DiscordAccountsPath = launchConfiguration["discordAccountsPath"].ToString();
            BadGiveawayPhrasesPath = launchConfiguration["badGiveawayPhrasesPath"].ToString();

            var delaysBeforeDiscordGuildsChecking = launchConfiguration["delayAfterDiscordGuildsChecking"].ToString().Split('-');
            var delaysBeforeFoundGiveaway = launchConfiguration["delayBeforeFoundGiveaway"].ToString().Split('-');
            var delaysEachDiscordAccountReact = launchConfiguration["delayEachDiscordAccountReact"].ToString().Split('-');

            switch (delaysBeforeDiscordGuildsChecking.Length)
            {
                case 1:
                    MaxDelayBeforeDiscordGuildsChecking = int.Parse(delaysBeforeDiscordGuildsChecking[0]);
                    break;
                case 2:
                    MinDelayBeforeDiscordGuildsChecking = int.Parse(delaysBeforeDiscordGuildsChecking[0]);
                    MaxDelayBeforeDiscordGuildsChecking = int.Parse(delaysBeforeDiscordGuildsChecking[1]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(delaysBeforeDiscordGuildsChecking), "Delay before checking all discord guilds entered incorrectly");
            }
            switch (delaysBeforeFoundGiveaway.Length)
            {
                case 1:
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
