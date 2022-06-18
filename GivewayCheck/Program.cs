using DiscordGivewayBot.Configurations;
using DiscordGivewayBot.Controllers;
using DiscordGivewayBot.Services;
using GivewayCheck.Controllers;
using GivewayCheck.Services;
using System;
using System.Diagnostics;
using System.IO;
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
        static DiscordRoleController discordRoleController = new DiscordRoleController();
        static ProxyService proxyService = new ProxyService();

        static async Task Main(string[] args)
        {
            try
            {
                var discordAccounts = (await discordAccountController.ReadAccountsAsync(configurations.DiscordAccountsPath)).ToList();
                var discordGiveawayBots = (await discordGiveawayBotController.ReadDiscordGiveawayBotsAsync(configurations.DiscordGiveawayBotsPath, discordAccounts.ToArray())).ToArray();

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                await LogService.LogMessageAsync("Started checking discord giveaway bots for giveaway");
                while (true)
                {
                    if (stopwatch.Elapsed > TimeSpan.FromMinutes(configurations.DelayBeforeDiscordAccountsWlRoleChecking - 1))
                    {
                        await LogService.LogMessageAsync("Checking discord accounts for WL...");
                        var wonWLDiscordAccounts = await discordRoleController.GetDiscordAccountsWithGuildRolesAsync(discordAccounts.ToArray(), discordGiveawayBots.SelectMany(discordGiveawayBot => discordGiveawayBot.Guilds).DistinctBy(discordGuild => discordGuild.Id).ToArray());
                        await File.WriteAllLinesAsync(LaunchConfigurations.WonWLsPath, wonWLDiscordAccounts.SelectMany(wonWLDiscordAccountGuildGroup => wonWLDiscordAccountGuildGroup.Value.Select(discordAccount => string.Join(';', wonWLDiscordAccountGuildGroup.Key.Name, discordAccount.Token))));
                        stopwatch.Restart();
                        await LogService.LogMessageAsync("Discord accounts for WL checked.");
                    }

                    await discordGiveawayController.CheckDiscordGiveawaysAndRumbleBattlesAsync(discordAccounts.ToArray(), discordGiveawayBots);
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
