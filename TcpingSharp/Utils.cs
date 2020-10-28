using System;
using System.Net;

namespace TcpingSharp
{
    public static class Utils
    {
        public static IPAddress[] ParseAddresses(string host, bool allowMultipleIps = true)
        {
            try
            {
                return new[] { IPAddress.Parse(host) };
            }
            catch
            {
                var addresses = Dns.GetHostAddresses(host);
                return allowMultipleIps ? addresses : new[] { addresses[0] };
            }
        }

        public static void EraseLine() => Console.Write($"\r{new string(' ', Console.BufferWidth)}\r");
    }
}