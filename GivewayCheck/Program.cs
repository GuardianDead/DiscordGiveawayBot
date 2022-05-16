﻿using GivewayCheck.Domain;
using GivewayCheck.Models;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace GivewayCheck
{
    static class Program
    {
        static private string currentPath = Environment.CurrentDirectory;
        static private string launchConfigurationPath = $@"{currentPath}/launchConfiguration.json";
        static private string logPath = $@"{currentPath}/log.txt";

        static private string lastLogMessage;

        static private string badPhrasesPath;
        static private string participateGiveawayPath;
        static private string endedGiveawayPath;
        static private string discordServersPath;
        static private string discordAccountsPath;

        static private int countAccountsForRead;

        static private int minDelayDiscordServersCheck;
        static private int maxDelayDiscordServersCheck;
        static private int minDelayDiscordAccountReact;
        static private int maxDelayDiscordAccountReact;

        static private List<string> badPhrases = new List<string>();
        static private List<string> participateGiveawayList = new List<string>();
        static private List<string> participateEndedGiveawayList = new List<string>();
        static private List<DiscordAccount> discordAccounts;
        static private List<DiscordServer> discordServers;

        static async Task Main(string[] args)
        {
            try
            {
                await ReadAllJsonConfigurationAsync();
                await ReadAllFiles();

                var discordRequest = new RestRequest();
                discordRequest.AddHeader("tts", false);
                while (true)
                {
                    lastLogMessage = $"[{DateTime.Now.ToLongTimeString()}] Начата проверка дискорд серверов на наличие гива...";
                    await WriteMessageInLogFileAsync(lastLogMessage);
                    Console.WriteLine(lastLogMessage);
                    var discordServerIndex = 0;
                    var discordAccountIndex = 0;
                    while (discordServerIndex < discordServers.Count)
                    {
                        discordRequest.AddHeader("authorization", discordAccounts[discordAccountIndex].Token);
                        RestClient restClient = CreateRestClient(discordAccounts[discordAccountIndex]?.Proxy);
                        discordRequest.Resource = $"https://discord.com/api/v9/guilds/{discordServers[discordServerIndex].Id}" +
                            $"/messages/search?author_id={discordServers[discordServerIndex].GiveawayBot.Id}";

                        var lastBotMessagesRespounce = await restClient.ExecuteGetAsync(discordRequest);
                        if (!lastBotMessagesRespounce.IsSuccessful)
                        {
                            discordAccountIndex++;
                            if (discordAccountIndex == discordAccounts.Count)
                                discordAccountIndex = 0;
                            continue;
                        }

                        var lastBotMessages = JArray.Parse(JObject.Parse(lastBotMessagesRespounce.Content)["messages"].ToString());
                        foreach (var message in lastBotMessages)
                        {
                            var giveawayPath = $@"{discordServers[discordServerIndex].Id}/{message.First["channel_id"]}/{message.First["id"]}";
                            await CheckMessageForGiveaway(message, giveawayPath, discordRequest, discordServerIndex);
                            await CheckMesssageForRumbleButtle(message, giveawayPath, discordRequest, discordServerIndex);
                            await CheckGiveawayForEnded(giveawayPath, message);
                        }

                        discordServerIndex++;
                    }

                    var delayInSecond = new Random().Next(minDelayDiscordServersCheck, maxDelayDiscordServersCheck);
                    if (delayInSecond != 0)
                        await WriteMessageInLogFileAsync($"[{DateTime.Now.ToLongTimeString()}] Спим {delayInSecond}с");
                    await Task.Delay(delayInSecond * 1000);
                }
            }
            catch (Exception ex)
            {
                lastLogMessage = $"Случилась непредвиденная ошибка: {ex.Message}";
                await WriteMessageInLogFileAsync(lastLogMessage);
                Console.WriteLine(lastLogMessage);
                Console.ReadLine();
            }
        }

        static private async Task WriteMessageInLogFileAsync(string message) => await File.AppendAllTextAsync(logPath, message + Environment.NewLine);
        static private async Task ReadAllJsonConfigurationAsync()
        {
            await WriteMessageInLogFileAsync($"[{DateTime.Now.ToLongTimeString()}] Читаем конфигурационный файл...");
            var launchConfiguration = JObject.Parse(await File.ReadAllTextAsync(launchConfigurationPath));
            participateGiveawayPath = launchConfiguration["participateGiveawayPath"].ToString();
            endedGiveawayPath = launchConfiguration["endedGiveawayPath"].ToString();
            discordServersPath = launchConfiguration["discordServersPath"].ToString();
            discordAccountsPath = launchConfiguration["discordAccountsPath"].ToString();
            countAccountsForRead = int.Parse(launchConfiguration["countAccountsForRead"].ToString());
            badPhrasesPath = launchConfiguration["badPhrasesPath"].ToString();
            var delaysDiscordServersCheck = launchConfiguration["delayBeforeDiscordServersCheck"].ToString().Split('-');
            var delaysDiscordAccountReact = launchConfiguration["delayBeforeDiscordAccountReact"].ToString().Split('-');
            if (delaysDiscordServersCheck.Length == 2)
            {
                minDelayDiscordServersCheck = int.Parse(delaysDiscordServersCheck.First());
                maxDelayDiscordServersCheck = int.Parse(delaysDiscordServersCheck.Last());
            }
            else if (delaysDiscordServersCheck.Length == 1)
                maxDelayDiscordServersCheck = int.Parse(delaysDiscordServersCheck.First());
            if (delaysDiscordAccountReact.Length == 2)
            {
                minDelayDiscordAccountReact = int.Parse(delaysDiscordAccountReact.First());
                maxDelayDiscordAccountReact = int.Parse(delaysDiscordAccountReact.Last());
            }
            else if (delaysDiscordAccountReact.Length == 1)
                maxDelayDiscordAccountReact = int.Parse(delaysDiscordAccountReact.First());
        }
        static private async Task ReadAllFiles()
        {
            discordServers = await ReadAllDiscordGiveawayServersAsync();
            discordAccounts = await ReadAllAwalableDiscordAccountsAsync(2, 4, countAccountsForRead + 1, 9);
            await WriteMessageInLogFileAsync($"[{DateTime.Now.ToLongTimeString()}] Проверяем и возможно читаем имеющиеся гивы...");
            if (File.Exists(participateGiveawayPath))
                participateGiveawayList = (await File.ReadAllLinesAsync(participateGiveawayPath)).ToList();
            await WriteMessageInLogFileAsync($"[{DateTime.Now.ToLongTimeString()}] Проверяем и возможно читаем имеющиеся законченые гивы...");
            if (File.Exists(endedGiveawayPath))
                participateEndedGiveawayList = (await File.ReadAllLinesAsync(endedGiveawayPath)).ToList();
            await WriteMessageInLogFileAsync($"[{DateTime.Now.ToLongTimeString()}] Проверяем и возможно читаем имеющиеся плохие фразы для гива...");
            if (File.Exists(badPhrasesPath))
                badPhrases = (await File.ReadAllLinesAsync(badPhrasesPath)).ToList();
        }
        static private async Task CheckMessageForGiveaway(JToken message, string giveawayPath, RestRequest discordRequest, int discordServerIndex)
        {
            if (!string.IsNullOrEmpty(message.First?["embeds"].First?["footer"]?["text"].ToString()) &&
                !string.IsNullOrEmpty(message.First?["embeds"].First?["author"]?["name"].ToString()))
            {
                if (DateTime.Now < DateTime.Parse(message.First["timestamp"].ToString()).AddDays(2) &&
                    !participateGiveawayList.Contains(giveawayPath) &&
                    (message.First["embeds"].First["footer"]["text"].ToString().Contains("Ends") ||
                    message.First["embeds"].First["footer"]["text"].ToString().Contains("winner")) &&
                    !badPhrases.Any(badPrhase => message.First["embeds"].First["author"]["name"].ToString().Contains(badPrhase)))
                {
                    await WriteMessageInLogFileAsync($"[{DateTime.Now.ToLongTimeString()}] Найден гив по всем походящим параметрам - {giveawayPath}");
                    await ReactMessageFromAllDiscordAccountAsync(discordRequest, discordServers[discordServerIndex], message);
                }
            }
        }
        private static async Task CheckMesssageForRumbleButtle(JToken message, string giveawayPath, RestRequest discordRequest, int discordServerIndex)
        {
            if (DateTime.Now < DateTime.Parse(message.First["timestamp"].ToString()).AddHours(1) &&
                !participateGiveawayList.Contains(giveawayPath) &&
                !string.IsNullOrEmpty(message.First?["embeds"].First?["description"]?.ToString()) &&
                message.First["embeds"].First["description"].ToString().Contains("Click the emoji below to join"))
            {
                await WriteMessageInLogFileAsync($"[{DateTime.Now.ToLongTimeString()}] Найден рамбл по всем походящим параметрам - {giveawayPath}");
                await ReactMessageFromAllDiscordAccountAsync(discordRequest, discordServers[discordServerIndex], message);
            }
        }
        static private async Task CheckGiveawayForEnded(string giveawayPath, JToken message)
        {
            if (participateGiveawayList.Contains(giveawayPath) &&
                !participateEndedGiveawayList.Contains(giveawayPath) &&
                !string.IsNullOrEmpty(message.First?["embeds"].First?["footer"]?["text"].ToString()) &&
                message.First["embeds"].First["footer"]["text"].ToString().Contains("Ended"))
            {
                Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] Этот {giveawayPath} гив окончен");
                participateGiveawayList.Remove(giveawayPath);
                participateEndedGiveawayList.Add(giveawayPath);
                await File.WriteAllLinesAsync(participateGiveawayPath, participateGiveawayList);
                await File.WriteAllLinesAsync(endedGiveawayPath, participateEndedGiveawayList);
            }
        }
        static private async Task ReactMessageFromAllDiscordAccountAsync(RestRequest discordRequest, DiscordServer discordServer, JToken message)
        {
            var channelId = message.First["channel_id"].ToString();
            var messageId = message.First["id"].ToString();
            var giveawayPath = $@"{discordServer.Id}/{channelId}/{messageId}";

            var addedReactDiscordAccountsCount = 0;
            discordRequest.Resource = $"https://discord.com/api/v9/channels/{channelId}/messages/{messageId}/reactions/{discordServer.GiveawayBot.Emoji}/@me";
            foreach (var discordAccount in discordAccounts)
            {
                RestClient client = CreateRestClient(discordAccount?.Proxy);
                discordRequest.AddOrUpdateHeader("authorization", discordAccount.Token);
                var resultRespounce = await client.ExecutePutAsync(discordRequest);
                if (!resultRespounce.IsSuccessful)
                {
                    if (resultRespounce.Content.Contains("Unauthorized"))
                        lastLogMessage = $"[{DateTime.Now.ToLongTimeString()}] Ошибка при реакте {discordAccount.Token} " +
                            $"этого сообщения {giveawayPath}: токен устарел";
                    else if (resultRespounce.Content.Contains("Access"))
                        lastLogMessage = $"[{DateTime.Now.ToLongTimeString()}] Ошибка при реакте {discordAccount.Token} " +
                            $"этого сообщения {giveawayPath}: этого аккаунта нет на сервере либо у него нет доступа к каналу гива";
                    else
                        lastLogMessage = $"[{DateTime.Now.ToLongTimeString()}] Ошибка при реакте {discordAccount.Token} " +
                            $"этого сообщения {giveawayPath}: {resultRespounce.Content}";
                    Console.WriteLine(lastLogMessage);
                    await WriteMessageInLogFileAsync(lastLogMessage);
                }
                else
                    addedReactDiscordAccountsCount++;
                await Task.Delay(new Random().Next(minDelayDiscordAccountReact, maxDelayDiscordAccountReact));
            }
            participateGiveawayList.Add($@"{discordServer.Id}/{channelId}/{messageId}");
            await File.WriteAllLinesAsync(participateGiveawayPath, participateGiveawayList);
            lastLogMessage = $"[{DateTime.Now.ToLongTimeString()}] {addedReactDiscordAccountsCount}/{discordAccounts.Count} " +
                    $"червя бахнули лайки - {discordServer.Id}/{channelId}/{messageId}";
            await WriteMessageInLogFileAsync(lastLogMessage);
            Console.WriteLine(lastLogMessage);
        }
        static private RestClient CreateRestClient(Proxy proxy)
        {
            if (proxy is not null)
            {
                var clientOptions = new RestClientOptions()
                {
                    Timeout = 0,
                    Proxy = new WebProxy(proxy.Address, int.Parse(proxy.Port))
                    {
                        Credentials = new NetworkCredential(proxy.Login, proxy.Password)
                    }
                };
                return new RestClient(clientOptions);
            }
            else
                return new RestClient();
        }
        static private async Task<List<DiscordServer>> ReadAllDiscordGiveawayServersAsync()
        {
            await WriteMessageInLogFileAsync($"[{DateTime.Now.ToLongTimeString()}] Читаем имеющиеся в файле дискорд сервера...");
            var stringJsonDiscordSevers = JObject.Parse(await File.ReadAllTextAsync(discordServersPath));
            var jsonArrayDiscordServers = JArray.Parse(stringJsonDiscordSevers["discordServers"].ToString());
            return jsonArrayDiscordServers
                .Select(discordServer => new DiscordServer(
                    id: (string)discordServer["id"],
                    name: (string)discordServer["name"],
                    giveawayBot: new DiscordGiveawayBot(
                        id: (string)discordServer["giveawayBot"]["id"],
                        emoji: (string)discordServer["giveawayBot"]["emoji"])))
                .ToList();
        }
        static private async Task<List<DiscordAccount>> ReadAllAwalableDiscordAccountsAsync(int fromRow, int fromColumn, int toRow, int toColumn)
        {
            await WriteMessageInLogFileAsync($"[{DateTime.Now.ToLongTimeString()}] Читаем имеющиеся в файле дискорд аккаунты...");
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var discordAccounts = new List<DiscordAccount>();
            var sheet = new ExcelPackage(discordAccountsPath).Workbook.Worksheets.First();
            var resultValues = (object[,])sheet.Cells[fromRow, fromColumn, toRow, toColumn].Value;
            for (int rowIndex = 0; rowIndex <= toRow - 2; rowIndex++)
            {
                if (string.IsNullOrEmpty(resultValues[rowIndex, 0]?.ToString()) || resultValues[rowIndex, 5].ToString() == "0")
                    continue;
                if (!string.IsNullOrEmpty(resultValues[rowIndex, 1]?.ToString()))
                {
                    var proxy = new Proxy(resultValues[rowIndex, 1].ToString(), resultValues[rowIndex, 2].ToString(),
                        resultValues[rowIndex, 3].ToString(), resultValues[rowIndex, 4].ToString());
                    discordAccounts.Add(new DiscordAccount(resultValues[rowIndex, 0].ToString(), proxy));
                }
                else
                    discordAccounts.Add(new DiscordAccount(resultValues[rowIndex, 0].ToString(), null));
            }
            discordAccounts.Reverse();
            return discordAccounts;
        }
    }
}
