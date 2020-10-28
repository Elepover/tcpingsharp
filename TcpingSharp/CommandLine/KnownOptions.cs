using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TcpingSharp.CommandLine
{
    public class KnownOptions
    {
        public static readonly Option HelpOption               = new Option(new[] {"-h", "-?"}, new[] {"--help"}, null, null);
        public static readonly Option TcpingTargetOption       = new Option(null, null, typeof(string), null);
        public static readonly Option PortOption               = new Option(new[] {"-p"}, new[] {"--port"}, typeof(ushort), 80);
        public static readonly Option TimeoutOption            = new Option(new[] {"-t"}, new[] {"--timeout"}, typeof(int), 5000);
        public static readonly Option ResolveMultipleIPsOption = new Option(new[] {"-m"}, new[] {"--multiple"}, null, null);
        public static readonly Option AnimateOption            = new Option(new[] {"-a"}, new[] {"--animate"}, null, null);
        public static readonly Option PeriodicalStatsOption    = new Option(new[] {"-s"}, new[] {"--stats"}, null, null);
        public static readonly Option RealRttOption            = new Option(new[] {"-r"}, new[] {"-rtt"}, null, null);
        
        /// <summary>
        /// Definition of default options. The boolean value in the tuple means whether it is required.<br />
        /// If it's not required but has default value, the option is then always treated as already specified.
        /// </summary>
        public static readonly ReadOnlyCollection<(bool, Option)> Options = new ReadOnlyCollection<(bool, Option)>(new List<(bool, Option)>
        {
            // OPTION DESC               REQUIRED?  REQUIRES VALUE?  HAS DEFAULT VALUE?
            // help                      no         no               no
            (false, HelpOption),
            // tcping target             yes        yes              no
            (true,  TcpingTargetOption),
            // port selection            yes        yes              yes, 80
            (true,  PortOption),
            // timeout                   yes        yes              yes, 5000ms
            (true,  TimeoutOption),
            // resolve multiple IPs      no         no               no
            (false, ResolveMultipleIPsOption),
            // animate                   no         no               no
            (false, AnimateOption),
            // periodically print stats  no         no               no
            (false, PeriodicalStatsOption),
            // display real rtt value    no         no               no
            (false, RealRttOption)
        });
    }
}