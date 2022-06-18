using DiscordGivewayBot.Data.Models;
using System;
using System.Collections.Generic;
using System.Net;

namespace DiscordGivewayBot.Services
{
    public class ProxyService
    {
        public ProxyService()
        {
        }

        public IEnumerable<Proxy> SortAliveProxy(IEnumerable<Proxy> proxies)
        {
            foreach (var proxy in proxies)
                if (CheckProxyForAlive(proxy))
                    yield return proxy;
        }
        public bool CheckProxyForAlive(Proxy proxy) => new WebProxy(proxy.Address, proxy.Port)
        {
            Credentials = new NetworkCredential(proxy.Login, proxy.Password)
        }.IsBypassed(new Uri("https://www.google.ru/"));
    }
}
