using System;
using System.Linq;

namespace TcpingSharp.CommandLine
{
    /// <summary>
    /// Representing a command line option
    /// </summary>
    public class Option
    {
        public Option(string[]? shortFlag, string[]? longFlag, Type? type, object? value)
        {
            ShortFlag = shortFlag;
            LongFlag = longFlag;
            Type = type;
            Value = value;
        }

        public bool IsTheSameAs(Option another)
        {
            if (!another.ShortFlag?.SequenceEqual(ShortFlag ?? new []{""}) ?? false) return false;
            if (!another.LongFlag?.SequenceEqual(LongFlag ?? new []{""}) ?? false) return false;
            return another.Type == Type;
        }

        public bool HasFlag => !(ShortFlag is null & LongFlag is null);
        /// <summary>
        /// Short flag of the option, like <c>-h</c>
        /// </summary>
        public string[]? ShortFlag { get; }
        /// <summary>
        /// Long flag of the option, like <c>--help</c>
        /// </summary>
        public string[]? LongFlag { get; }
        /// <summary>
        /// Data type of the option. Set to <see langword="null"/> if no value is required.
        /// </summary>
        public Type? Type { get; }
        /// <summary>
        /// The option's data.
        /// </summary>
        public object? Value { get; set; }
    }
}