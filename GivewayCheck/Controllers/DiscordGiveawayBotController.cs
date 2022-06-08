using DiscordGivewayBot.Data.Models.Entities;
using GivewayCheck.Services;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GivewayCheck.Controllers
{
    public class DiscordGiveawayBotController
    {
        public DiscordGiveawayBotController()
        {
        }

        public async Task<IEnumerable<DiscordGiveawayBot>> ReadDiscordGiveawayBotsAsync(string discordGiveawayBotsPath)
        {
            await LogService.LogMessageAsync("Reading discord giveaway bots from a file...");
            var discordGiveawayBotsJson = JObject.Parse(await File.ReadAllTextAsync(discordGiveawayBotsPath));
            var discordGiveawayBotsJsonArray = JArray.Parse(discordGiveawayBotsJson["discordGiveawayBots"].ToString());
            return discordGiveawayBotsJsonArray
                .Select(discordGiveawayBotJson => new DiscordGiveawayBot(long.Parse(discordGiveawayBotJson["id"].ToString()),
                new DiscordGuild(long.Parse(discordGiveawayBotJson["discordServer"]["id"].ToString()), discordGiveawayBotJson["discordServer"]["name"].ToString()), discordGiveawayBotJson["emoji"].ToString()))
                .ToList();
        }
    }
}
