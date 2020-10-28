using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace TcpingSharp.CommandLine
{
    public class CommandLineParser
    {
        /// <summary>
        /// Parse a command line argument to options, 
        /// </summary>
        /// <param name="args">Arguments to resolve</param>
        /// <param name="knownOptions"></param>
        /// <returns></returns>
        public ReadOnlyCollection<Option> Parse(string[] args, ICollection<(bool, Option)> knownOptions)
        {
            // phase 1: parse and add specified, resolvable and known args
            var optionList = new List<Option>();
            (bool, Option)? currentOption = null;
            foreach (var arg in args)
            {
                // check argument type
                if (arg.StartsWith("-"))
                {
                    // it is a flagged argument
                    // check if previous argument is awaiting for value
                    if (!(currentOption is null))
                    {
                        // yes! oh no, it's awaiting value, this is not supposed to happen
                        throw new ArgumentException($"Option {currentOption.Value.Item2.LongFlag?[0]} requires a value.");
                    }
                    
                    // search in knownOptions
                    var matchingOption = knownOptions.FirstOrDefault(x =>
                        x.Item2.HasFlag && ((x.Item2.LongFlag?.Contains(arg) ?? false) || (x.Item2.ShortFlag?.Contains(arg) ?? false)));
                    if (matchingOption == default)
                    {
                        // no, it's invalid, just skip it.
                        currentOption = null;
                    }
                    else
                    {
                        // yes, matched
                        // but we need to check if it expects value
                        if (matchingOption.Item2.Type is null)
                        {
                            // not needed! this is done!
                            optionList.Add(matchingOption.Item2);
                        }
                        else
                        {
                            // yes, set awaiting flag
                            currentOption = matchingOption;
                        }
                    }
                }
                else
                {
                    // it is a flag-less argument
                    // check if previous argument is awaiting for value
                    if (!(currentOption is null))
                    {
                        // put this value there
                        currentOption.Value.Item2.Value = arg;
                        // add it to the options list
                        optionList.Add(currentOption.Value.Item2);
                        currentOption = null;
                    }
                    else
                    {
                        // no previous argument awaiting! it's a standalone argument
                        // search in knownOptions to see if there's any flag-less argument
                        var matchingOption = knownOptions.FirstOrDefault(x => !x.Item2.HasFlag);
                        if (matchingOption == default)
                        {
                            // not found, ditch it
                        }
                        else
                        {
                            // yes, set it
                            matchingOption.Item2.Value = arg;
                            // add it
                            optionList.Add(matchingOption.Item2);
                        }
                    }
                }
            }
            
            // phase 2: check knownOptions to find if any mandatory option is missing
            foreach (var option in knownOptions)
            {
                // is it set?
                if (!optionList.Any(x => option.Item2.IsTheSameAs(x)))
                {
                    // no it isn't!
                    // is it required?
                    if (option.Item1)
                    {
                        // yes it is! does it have a default value?
                        if (option.Item2.Value is null)
                        {
                            // no it doesn't! user need to specify a value
                            var optionArg = option.Item2.LongFlag?[0];
                            if (string.IsNullOrEmpty(optionArg)) optionArg = "(anonymous)";
                            throw new ArgumentException($"Option {optionArg} is required.");
                        }
                        // yes it has! then we just add a new value
                        optionList.Add(option.Item2);
                    }
                    // no it isn't required, fine then
                }
                // then it's fine, no worries here
            }
            
            // all finished. return it
            return new ReadOnlyCollection<Option>(optionList);
        }
    }
}