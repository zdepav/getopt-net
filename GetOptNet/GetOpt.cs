using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GetOptNet {

    public static class GetOpt {

        private class Option {

            public readonly string FirstName;

            public readonly bool HasValue;

            public Option(string firstName, bool hasValue) {
                FirstName = firstName;
                HasValue = hasValue;
            }
        }

        private static readonly Regex optionsDefinitionRegex = new Regex(
            @"(?:[a-z]|[a-z][a-z0-9-]{1,39})(?:\|(?:[a-z]|[a-z][a-z0-9-]{1,39}))*:?(?:,(?:[a-z]|[a-z][a-z0-9-]{1,39})(?:\|(?:[a-z]|[a-z][a-z0-9-]{1,39}))*:?)*",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant
        );

        public static string[] Parse(string[] args, string options, bool singleDashLongOptions = false) {
            if (args is null) {
                throw new ArgumentNullException(nameof(args));
            }
            if (options is null) {
                throw new ArgumentNullException(nameof(options));
            }
            if (!optionsDefinitionRegex.IsMatch(options)) {
                throw new FormatException("Options are not in a valid format.");
            }
            var (shortOptions, longOptions) = ParseOptions(options, singleDashLongOptions);






            return Array.Empty<string>();
        }

        private static (
            Dictionary<char, Option>,
            Dictionary<string, Option>
        ) ParseOptions(
            string options,
            bool singleDashLongOptions
        ) {
            var shortOptions = new Dictionary<char, Option>();
            var longOptions = new Dictionary<string, Option>();
            foreach (var opt in options.Split(',')) {
                Option? option = null;
                foreach (var variant in opt.TrimEnd(':').Split('|')) {
                    if (variant.Length == 1) {
                        if (shortOptions.ContainsKey(variant[0])) {
                            throw new ArgumentException(
                                $"Duplicate short option definition (-{variant[0]})",
                                nameof(options)
                            );
                        }
                        if (option is null) {
                            option = new Option('-' + variant, opt[^1] == ':');
                        }
                        shortOptions.Add(variant[0], option);
                    } else {
                        var fullName = (singleDashLongOptions ? "-" : "--") + variant;
                        if (longOptions.ContainsKey(variant)) {
                            throw new ArgumentException(
                                $"Duplicate long option definition ({fullName})",
                                nameof(options)
                            );
                        }
                        if (option is null) {
                            option = new Option(fullName, opt[^1] == ':');
                        }
                        longOptions.Add(variant, option);
                    }
                }
            }
            return (shortOptions, longOptions);
        }
    }
}
