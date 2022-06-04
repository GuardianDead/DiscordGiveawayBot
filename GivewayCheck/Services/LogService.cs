using DiscordGivewayBot.Domain.Constants;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GivewayCheck.Services
{
    static public class LogService
    {
        static public async Task LogMessageAsync(string message)
        {
            Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] {message}");
            await File.AppendAllTextAsync(ConstantPaths.logPath, $"[{DateTime.Now.ToLongTimeString()}] {message} {Environment.NewLine}");
        }
        static public void LogMessage(string message)
        {
            Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] {message}");
            File.AppendAllText(ConstantPaths.logPath, $"[{DateTime.Now.ToLongTimeString()}] {message} {Environment.NewLine}");
        }
    }
}
