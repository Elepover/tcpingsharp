using System;
using System.Net;
using System.Threading.Tasks;

namespace TcpingSharp
{
    internal static class Utils
    {
        public static async Task<IPAddress[]> ParseAddressesAsync(string host, bool allowMultipleIps = true)
        {
            try
            {
                return new[] {IPAddress.Parse(host)};
            }
            catch
            {
                var addresses = await Dns.GetHostAddressesAsync(host);
                return allowMultipleIps ? addresses : new[] {addresses[0]};
            }
        }

        public static void EraseLine() => Console.Write($"\r{new string(' ', Console.BufferWidth)}\r");
    }
}
