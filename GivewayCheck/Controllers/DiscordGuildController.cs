using GivewayCheck.Models;
using GivewayCheck.Services;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GivewayCheck.Controllers
{
    public class DiscordGuildController
    {
        public DiscordGuildController()
        {
        }

        public async Task<IEnumerable<DiscordGuild>> ReadDiscordGuildsAsync(string discordServersPath)
        {
            await LogService.LogMessageAsync("Reading discord guilds from a file...");
            var discordSeversJson = JObject.Parse(await File.ReadAllTextAsync(discordServersPath));
            var discordServersJsonArray = JArray.Parse(discordSeversJson["discordGuilds"].ToString());
            return discordServersJsonArray
                .Select(discordServer => new DiscordGuild(discordServer["id"].ToString(), discordServer["name"].ToString(), new DiscordGiveawayBot(discordServer["giveawayBot"]["id"].ToString(), discordServer["giveawayBot"]["emoji"].ToString())))
                .ToList();
        }
    }
}
