using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TcpingSharp.CommandLine;
using TcpingSharp.Tcping;

namespace TcpingSharp
{
    public static class Program
    {
        private const int PrintStatsInterval = 25;
        private const string ApplicationVersion = "1.0.26b";
        private static int _exitCounter = 0;
        private static int _eventCounter = 0;
        private static Stopwatch _stopwatch = new Stopwatch();

        private static void PrintHelp()
        {
            Console.WriteLine(
                "TcpingSharp " + ApplicationVersion + Environment.NewLine +
                "usage: tcping target [options]" + Environment.NewLine +
                "  target: tcping target, can be a domain, hostname or IP" + Environment.NewLine +
                "  options:" + Environment.NewLine +
                "    -h, -?, --help          Print this message." + Environment.NewLine +
                "    -p, --port target_port  Set target port, default value is 80." + Environment.NewLine +
                "    -t, --timeout timeout   Set timeout, default value is 5000 (ms)" + Environment.NewLine +
                "    -m, --multiple          Allow pinging multiple IPs simultaneously." + Environment.NewLine +
                "    -a, --animate           Animate output into a single line, incompatible" + Environment.NewLine +
                "                              with -m option." + Environment.NewLine +
                "    -s, --stats             Periodically print statistics." + Environment.NewLine +
                "    -r, --rtt               Instead of showing time spent establishing a" + Environment.NewLine +
                "                              TCP connection (~2x RTT), show half of the" + Environment.NewLine +
                "                              value (~actual RTT)."
            );
        }

        private static void WriteError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.Write(message);
            Console.ResetColor();
        }

        private static void WriteErrorLine(string message)
        {
            WriteError(message + Environment.NewLine);
        }

        private static void PrintStats(ReadOnlyDictionary<IPAddress, ReadOnlyCollection<double>> stats, bool realRtt)
        {
            foreach (var (ipAddress, rawStats) in stats)
            {
                var extractedData = new List<double>(rawStats);

                Console.WriteLine($"--- {ipAddress} tcping statistics ---");

                // trim 0 (failed) from results
                var failed = extractedData.Count(x => x == 0);
                extractedData.RemoveAll(x => x == 0);
                var succeeded = extractedData.Count;
                var total = failed + succeeded;

                // calculate result and output
                Console.WriteLine(
                    $"{total} connections attempted, {succeeded} succeeded, {(double)failed / total * 100:0.0}% failure chance");
                if (extractedData.Count == 0)
                    WriteErrorLine("stats unavailable: no successful connection attempts");
                else
                    Console.WriteLine(
                        $"{(realRtt ? "" : "2x ")}round-trip min/avg/max/stddev = {extractedData.Min():0.000}/{extractedData.Average():0.000}/{extractedData.Max():0.000}/{extractedData.StdDev():0.000}ms");
                Console.WriteLine($"time spent: {_stopwatch.Elapsed:c}");
            }
        }

        private static async Task Main(string[] args)
        {
            #region Parse arguments

            ParsedOptions opts;
            try
            {
                var options = new CommandLineParser().Parse(args, KnownOptions.Options);
                opts = new ParsedOptions(options);
            }
            catch (Exception ex)
            {
                PrintHelp();
                Environment.Exit(ex.HResult);
                return;
            }

            #endregion

            #region Resolve IP addresses

            IPAddress[] addresses;
            try
            {
                Console.Write("Resolving addresses...");
                addresses = await Utils.ParseAddressesAsync(opts.TcpingTarget, opts.ResolveMultipleIPs);
                Utils.EraseLine();
            }
            catch (Exception ex)
            {
                Utils.EraseLine();
                WriteErrorLine($"Supplied hostname ({opts.TcpingTarget}) cannot be resolved: {ex}");
                Environment.Exit(ex.HResult);
                return;
            }

            #endregion

            #region Print header

            Console.Write($"TCPING {opts.TcpingTarget}:{opts.Port} ");
            if (addresses.Length > 1)
            {
                Console.WriteLine($"({addresses.Length} IP{addresses.Length.S()}: {addresses.SpreadToString()})");
                if (opts.Animate)
                {
                    opts.Animate = false;
                    Console.WriteLine("warning: animation disabled for multiple IPs.");
                }
            }
            else
            {
                var addr = addresses[0];
                Console.WriteLine(
                    $"({addr}): {addr.AddressFamily switch {AddressFamily.InterNetwork => "IPv4, connect", AddressFamily.InterNetworkV6 => "IPv6, connect", _ => "?"}}");
            }

            Console.Write("options: ");
            if (opts.Animate)
                Console.Write("animated, ");
            if (opts.PeriodicalStats)
                Console.Write($"periodical ({PrintStatsInterval}) stats enabled, ");
            if (opts.RealRtt)
                Console.Write("RTT");
            else
                Console.Write("2xRTT");
            Console.WriteLine();

            #endregion

            #region Initialize tcping client

            var client = new TcpingClient(addresses, opts.Port) {RealRtt = opts.RealRtt, Timeout = opts.Timeout};

            #endregion

            #region Subscribe event handlers

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                _exitCounter++;
                var remainingPresses = 3 - _exitCounter;
                if (remainingPresses <= 0)
                {
                    WriteErrorLine("Terminating by force...");
                    Environment.Exit(1);
                }

                Utils.EraseLine();
                Console.WriteLine(
                    $"Stopping... Press {remainingPresses} time{remainingPresses.S()} more to perform force quit.");
                client.Stop(true);
                eventArgs.Cancel = true;
            };

            client.TcpingResponded += (sender, eventArgs) =>
            {
                var message = eventArgs.ToConsoleMessage(Console.BufferWidth, opts.Animate);
                if (eventArgs.IsSuccessful)
                {
                    Console.Write(message);
                }
                else
                {
                    WriteError(message);
                }

                if (opts.PeriodicalStats)
                {
                    _eventCounter++;
                    if (_eventCounter >= PrintStatsInterval)
                    {
                        _eventCounter %= PrintStatsInterval;
                        Utils.EraseLine();
                        PrintStats(client.Stats, opts.RealRtt);
                    }
                }
            };

            #endregion

            #region Start working

            // start client
            client.Start();
            _stopwatch.Start();

            // wait thread exit
            while (client.IsActive)
            {
                await Task.Delay(100);
            }

            #endregion

            #region Finish and show results

            // display summary
            PrintStats(client.Stats, opts.RealRtt);

            #endregion
        }
    }
}
