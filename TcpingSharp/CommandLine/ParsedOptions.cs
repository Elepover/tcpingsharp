using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace TcpingSharp.CommandLine
{
    public class ParsedOptions
    {
        public ParsedOptions() {}
        
        public ParsedOptions(ReadOnlyCollection<Option> raw)
        {
            Help               =            raw.Any(           x => KnownOptions.HelpOption.IsTheSameAs(x));
            TcpingTarget       =            raw.FirstOrDefault(x => !x.HasFlag)?.Value?.ToString() ?? throw new ArgumentNullException();
            Port               = int.Parse((raw.FirstOrDefault(x => KnownOptions.PortOption.IsTheSameAs(x))?.Value ?? throw new ArgumentNullException()).ToString() ?? throw new FormatException());
            Timeout            = int.Parse((raw.FirstOrDefault(x => KnownOptions.TimeoutOption.IsTheSameAs(x))?.Value ?? throw new ArgumentNullException()).ToString() ?? throw new FormatException());
            ResolveMultipleIPs =            raw.FirstOrDefault(x => KnownOptions.ResolveMultipleIPsOption.IsTheSameAs(x)) != default;
            Animate            =            raw.FirstOrDefault(x => KnownOptions.AnimateOption.IsTheSameAs(x)) != default;
            PeriodicalStats    =            raw.FirstOrDefault(x => KnownOptions.PeriodicalStatsOption.IsTheSameAs(x)) != default;
            RealRtt            =            raw.FirstOrDefault(x => KnownOptions.RealRttOption.IsTheSameAs(x)) != default;
        }

        public bool Help { get; set; } = false;
        public string TcpingTarget { get; set; } = string.Empty;
        public int Port { get; set; } = 80;
        public int Timeout { get; set; } = 5000;
        public bool ResolveMultipleIPs { get; set; } = false;
        public bool Animate { get; set; } = false;
        public bool PeriodicalStats { get; set; } = false;
        public bool RealRtt { get; set; } = false;
    }
}