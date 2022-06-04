using GivewayCheck.Domain;
using GivewayCheck.Models;
using GivewayCheck.Services;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace GivewayCheck.Controllers
{
    public class DiscordAccountController
    {
        public DiscordAccountController()
        {
        }

        public async Task<IEnumerable<DiscordAccount>> ReadAccountsAsync(string discordAccountsPath)
        {
            await LogService.LogMessageAsync("Reading discord accounts from a file...");
            var discordAccountStrings = await File.ReadAllLinesAsync(discordAccountsPath);

            var discordAccounts = new List<DiscordAccount>();
            foreach (var discordAccountString in discordAccountStrings)
            {
                var discordAccountStringValues = discordAccountString.Split(';', ':');
                Proxy discordAccountProxy = null;

                if (discordAccountStringValues.Length != 1)
                    discordAccountProxy = new Proxy(discordAccountStringValues[1], int.Parse(discordAccountStringValues[2]), discordAccountStringValues[3], discordAccountStringValues[4]);

                discordAccounts.Add(new DiscordAccount(discordAccountStringValues[0], discordAccountProxy));
            }

            return discordAccounts;
        }
    }
}
