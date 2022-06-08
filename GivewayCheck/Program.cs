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
        static DiscordGiveawayBotController discordGiveawayBotController = new DiscordGiveawayBotController();
        static DiscordGiveawayController discordGiveawayController = new DiscordGiveawayController();

        static async Task Main(string[] args)
        {
            try
            {
                var discordAccounts = await discordAccountController.ReadAccountsAsync(configurations.DiscordAccountsPath);
                var discordGiveawayBots = await discordGiveawayBotController.ReadDiscordGiveawayBotsAsync(configurations.DiscordGiveawayBotsPath);
                await LogService.LogMessageAsync("Started checking discord giveaway bots for giveaway");
                while (true)
                {
                    await discordGiveawayController.CheckDiscordGiveawaysAndRumbleBattlesAsync(discordAccounts.ToArray(), discordGiveawayBots.ToArray());
                    await LogService.LogMessageAsync($"Discord giveaway bots checked.");

                    var delayInSecond = new Random().Next(configurations.MinDelayBeforeDiscordGiveawayBotsChecking, configurations.MaxDelayBeforeDiscordGiveawayBotsChecking);
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
