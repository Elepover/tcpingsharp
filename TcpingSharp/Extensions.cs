using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TcpingSharp.Tcping;

namespace TcpingSharp
{
    public static class Extensions
    {
        public static string S(this int num) => num == 1 ? "" : "s";

        public static string SpreadToString(this IEnumerable enumerable)
        {
            var sb = new StringBuilder();
            foreach (var obj in enumerable)
            {
                sb.Append(obj);
                sb.Append(", ");
            }

            sb.Remove(sb.Length - 2, 2);
            return sb.ToString();
        }

        // https://stackoverflow.com/a/3141731
        public static double StdDev(this IEnumerable<double> src)
        {
            if (src is null) throw new ArgumentNullException(nameof(src));

            double standardDeviation = 0;

            if (!src.Any()) return standardDeviation;
            // Compute the average.
            var avg = src.Average();

            // Perform the Sum of (value-avg)_2_2.
            var sum = src.Sum(d => Math.Pow(d - avg, 2));

            // Put it all together.
            standardDeviation = Math.Sqrt((sum) / (src.Count() - 1));

            return standardDeviation;
        }

        public static string ToConsoleMessage(this TcpingRespondedEventArgs args, int width = 0, bool animate = false)
        {
            var sb = new StringBuilder();
            if (animate) sb.Append("\r");
            sb.Append(args.IsSuccessful
                ? $"connected to {args.Address}:{args.Port}: seq={args.Sequence}, time={args.Time:0.000}ms"
                : $"{args.Address}:{args.Port}: code=0x{args.Exception?.HResult ?? -1:x8}, seq={args.Sequence}, time={args.Time:0.000}ms, msg={args.Exception?.Message ?? "unknown"}");
            if (animate) sb.Append(new string(' ', Math.Max(0, width - sb.Length - 1)));
            if (!animate) sb.Append(Environment.NewLine);
            return sb.ToString();
        }
    }
}
