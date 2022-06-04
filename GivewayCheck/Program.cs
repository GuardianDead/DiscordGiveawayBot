using DiscordGivewayBot.Configurations;
using GivewayCheck.Controllers;
using GivewayCheck.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GivewayCheck
{
    public class Program
    {
        static LaunchConfigurations configurations = new LaunchConfigurations();
        static DiscordAccountController discordAccountController = new DiscordAccountController();
        static DiscordGuildController discordGuildController = new DiscordGuildController();
        static DiscordGiveawayController discordGiveawayController = new DiscordGiveawayController();

        static async Task Main(string[] args)
        {
            try
            {
                var discordAccounts = await discordAccountController.ReadAccountsAsync(configurations.DiscordAccountsPath);
                var discordGuilds = await discordGuildController.ReadDiscordGuildsAsync(configurations.DiscordGuildsPath);
                await LogService.LogMessageAsync("Started checking discord guilds for giveaway");
                while (true)
                {
                    await discordGiveawayController.CheckGiveawaysAsync(discordAccounts.ToArray(), discordGuilds.ToArray());

                    await LogService.LogMessageAsync($"Discord guilds checked.");
                    var delayInSecond = new Random().Next(configurations.MinDelayBeforeDiscordGuildsChecking, configurations.MaxDelayBeforeDiscordGuildsChecking);
                    await LogService.LogMessageAsync($"Sleep {delayInSecond}s...");
                    await Task.Delay(delayInSecond * 1000);
                }
            }
            catch (Exception exception)
            {
                await LogService.LogMessageAsync($"Unexpected error during the operation of the application: {exception.Message} {exception.InnerException}");
                Console.ReadLine();
            }
        }
    }
}
