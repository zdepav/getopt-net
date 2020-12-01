#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Parser = System.Func<string, string, object?>;

namespace GetOptNet {

    /// <summary>
    /// Class used to parse program arguments
    /// </summary>
    public static partial class GetOpt {

        internal class Option {

            internal enum OptionValueType { None, String, Int, Double, DateTime, File, Directory }

            public readonly string FullFirstName;

            public readonly string FirstName;

            public readonly bool CanRepeat;

            public readonly OptionValueType ValueType;

            public bool Found { get; private set; }

            public string? Value { get; private set; }

            public readonly List<string>? Values;

            public bool HasValue => ValueType != OptionValueType.None;

            public Parser? DefaultValueParser =>
                ValueType switch {
                    OptionValueType.Int       => DefaultParsers.IntParser,
                    OptionValueType.Double    => DefaultParsers.DoubleParser,
                    OptionValueType.DateTime  => DefaultParsers.DateTimeParser,
                    OptionValueType.File      => DefaultParsers.FileInfoParser,
                    OptionValueType.Directory => DefaultParsers.DirectoryInfoParser,
                    _                         => null
                };

            public Option(string firstName, bool canRepeat, OptionValueType type) {
                FullFirstName = firstName;
                FirstName = firstName.TrimStart('-');
                CanRepeat = canRepeat;
                ValueType = type;
                Found = false;
                Value = null;
                Values = canRepeat ? new List<string>() : null;
            }

            public void Find(string name, string? value = null) {
                if (!CanRepeat && Found) {
                    throw new InvalidOptionsException($"Option '{name}' can't repeat.");
                }
                if (HasValue) {
                    if (CanRepeat) {
                        Values!.Add(value!);
                    } else {
                        Value = value;
                    }
                } else if (CanRepeat) {
                    Values!.Add(string.Empty);
                }
                Found = true;
            }

            public object? ParseValue(Parser? parser = null) {
                if (!HasValue) {
                    return CanRepeat ? Values!.Count : (object)true;
                } else if (parser != null) {
                    return CanRepeat
                        ? Values!.Select(v => parser(FirstName, v)).ToArray()
                        : parser(FirstName, Value!);
                } else {
                    return CanRepeat ? Values!.ToArray() : (object)Value!;
                }
            }
        }

        private static readonly Regex
            optionsDefinitionRegex = new Regex(
                @"^[a-z0-9]+(?:-[a-z0-9]+)*(?:\|[a-z0-9]+(?:-[a-z0-9]+)*)*" +
                @"(?<modifier>:[isfdnt]?!\+|:[isfdnt]?\+?!?|!:[isfdnt]?\+|!\+?(?::[isfdnt]?)?|\+:[isfdnt]?!|\+!?(?::[isfdnt]?)?)?$",
                RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant
            ),
            shortOptionRegex = new Regex(
                @"^-(?<noValueNames>[a-z0-9]*)(?<lastName>[a-z0-9])(?:=(?<value>.*))?$",
                RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant
            ),
            longOptionRegex = new Regex(
                @"^(?<fullName>-+(?<name>[a-z0-9]+(?:-[a-z0-9]+)*))(?:=(?<value>.*))?$",
                RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant
            );

        /// <summary>
        /// Parses program arguments based on <paramref name="options"/>.
        /// </summary>
        /// <param name="args">Program arguments to process.</param>
        /// <param name="options">
        /// List of allowed options separated by commas (,).<br/>
        /// Each option can have multiple names, separated by a vertical bar (|). A name is either a
        /// single letter or digit (for short options) or a sequence of letters and digits,
        /// optionally with words separated by hyphens (for long options). Name can't start or end
        /// with a hyphen, no two hyphens can be next to each other. Option name used in
        /// <paramref name="parsers"/> and in the returned object is the first one specified.<br/>
        /// Option can also have modifiers that are placed after the last name and can be in any
        /// order. Allowed modifiers are:
        /// <list type="table">
        ///   <listheader>
        ///     <term>modifier</term>
        ///     <description>meaning</description>
        ///   </listheader>
        ///   <item>
        ///     <term>!</term>
        ///     <description>required option</description>
        ///   </item>
        ///   <item>
        ///     <term>:</term>
        ///     <description>requires value</description>
        ///   </item>
        ///   <item>
        ///     <term>+</term>
        ///     <description>option can be used repeatedly</description>
        ///   </item>
        /// </list>
        /// &quot;requires value&quot; modifier can be optionally followed by a letter, specifying
        /// the type of the value. If type is specified and no parser is supplied for this option, a
        /// default one is used. Accepted types are:
        /// <list type="table">
        ///   <listheader>
        ///     <term>letter</term>
        ///     <description>.NET type</description>
        ///   </listheader>
        ///   <item>
        ///     <term>s</term>
        ///     <description><see cref="string"/> (default type for option values)</description>
        ///   </item>
        ///   <item>
        ///     <term>i</term>
        ///     <description><see cref="int"/></description>
        ///   </item>
        ///   <item>
        ///     <term>n</term>
        ///     <description><see cref="double"/></description>
        ///   </item>
        ///   <item>
        ///     <term>t</term>
        ///     <description>
        ///       <see cref="DateTime"/> (in any format supported by DateTime.TryParse)
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <term>f</term>
        ///     <description><see cref="FileInfo"/> (value must be a valid path)</description>
        ///   </item>
        ///   <item>
        ///     <term>d</term>
        ///     <description><see cref="DirectoryInfo"/> (value must be a valid path)</description>
        ///   </item>
        /// </list>
        /// </param>
        /// <param name="autoAddHelpAndVersionOptions">
        /// If true, automatically adds --help and --version options if they are not specified in
        /// <paramref name="options"/>.<br/>
        /// Default: true
        /// </param>
        /// <param name="ignoreRequiredOnHelpAndVersion">
        /// If true and <paramref name="args"/> contains --help or --version options,
        /// &quot;required&quot; option modifier is ignored.<br/>
        /// Default: true
        /// </param>
        /// <param name="singleDashLongOptions">
        /// If true, all options are long options and are prefixed with a single hyphen.<br/>
        /// If false, short options are prefixed with a single hyphen and can be used in groups,
        /// long options are prefixed with two hyphens.<br/>
        /// Default: false
        /// </param>
        /// <param name="valuesAfterOptions">
        /// If true, non-option arguments must appear after the last option argument.<br/>
        /// Default: false
        /// </param>
        /// <param name="includeUnusedOptions">
        /// If true, options that do not appear in <paramref name="args"/> are included in the
        /// returned object with null as their value.<br/>
        /// If false, options that do not appear in <paramref name="args"/> do not appear in the
        /// returned object.<br/>
        /// Default: false
        /// </param>
        /// <param name="parsers">
        /// A dictionary with functions used to parse option values. Keys are option names, values
        /// are functions with two string arguments (option name, value), that return the parsed
        /// value.
        /// </param>
        /// <returns>
        /// A <see cref="ParseOptions"/> object with options and values extracted from
        /// <paramref name="args"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="args"/> or <paramref name="options"/> is null.
        /// </exception>
        /// <exception cref="InvalidOptionsException">
        /// Thrown when <paramref name="args"/> contains invalid options
        /// </exception>
        /// <para><b>Remarks</b></para>
        /// <remarks>
        /// Options string can not contain any whitespace, only ASCII characters are supported.<br/>
        /// Options without a value modifier have <see cref="bool"/> type, with true as their value.
        /// <br/>
        /// Repeatable options without a value modifier have <see cref="int"/> type, with number of
        /// occurences as their value.
        /// </remarks>
        /// <para><b>Examples</b></para>
        /// <example><code>
        /// // Simple short options example
        /// 
        /// var parsedArgs = GetOpt.Parse(args, &quot;n,e,E&quot;);
        /// </code></example>
        /// <example><code>
        /// // Multiple names and typed value modifier example
        /// 
        /// var optionsString = &quot;date|d:s,file|f:f,reference|r:f,set|s,utc|universal|u&quot;;
        /// var parsedArgs = GetOpt.Parse(args, optionsString);
        /// </code></example>
        /// <example><code>
        /// // Custom parser example
        /// 
        /// var parsers = new Dictionary&lt;string, Func&lt;string, string, object?&gt;&gt; {
        ///     [&quot;file&quot;] = (_, path) =>
        ///         File.Exists(path)
        ///             ? Path.GetFullPath(path)
        ///             : throw new FileNotFoundException($&quot;File \&quot;{path}\&quot; not found.&quot;)
        /// };
        /// var parsedArgs = GetOpt.Parse(args, &quot;file|f:f&quot;, parsers: parsers);
        /// </code></example>
        /// <seealso cref="Parse(string[],bool,bool)"/>
        /// <seealso cref="ParsedArgs"/>
        public static ParsedArgs Parse(
            string[] args,
            string options,
            bool autoAddHelpAndVersionOptions = true,
            bool ignoreRequiredOnHelpAndVersion = true,
            bool singleDashLongOptions = false,
            bool valuesAfterOptions = false,
            bool includeUnusedOptions = false,
            Dictionary<string, Parser>? parsers = null
        ) {
            if (args is null) {
                throw new ArgumentNullException(nameof(args));
            }
            if (options is null) {
                throw new ArgumentNullException(nameof(options));
            }
            var (allOpts, requiredOpts, shortOpts, longOpts) =
                ParseOptions(options, singleDashLongOptions, autoAddHelpAndVersionOptions);

            var parsedOpts = new Dictionary<string, Option>();
            var retVals = new List<string>();
            var parsingOptions = true;
            var valueFound = false;
            for (var i = 0; i < args.Length; ++i) {
                var arg = args[i];
                if (!parsingOptions) {
                    retVals.Add(arg);
                    continue;
                }
                if (!arg.StartsWith("-") || arg.Length < 2) {
                    if (valuesAfterOptions) {
                        parsingOptions = false;
                        valueFound = true;
                    }
                    retVals.Add(arg);
                    continue;
                }
                if (arg == "--") {
                    if (valueFound && valuesAfterOptions) {
                        retVals.Add(arg);
                    }
                    parsingOptions = false;
                    continue;
                }
                if (singleDashLongOptions) {
                    if (arg.StartsWith("--")) {
                        throw new InvalidOptionsException($"Invalid option '{arg}'.");
                    }
                    if (arg.StartsWith("-")) {
                        if (valueFound && valuesAfterOptions) {
                            throw new InvalidOptionsException(
                                "Options must appear before non-option arguments."
                            );
                        }
                        ParseLongOption(args, longOpts, ref i, arg, parsedOpts);
                        continue;
                    }
                } else {
                    if (arg.StartsWith("---")) {
                        throw new InvalidOptionsException($"Invalid option '{arg}'.");
                    }
                    if (arg.StartsWith("--")) {
                        if (valueFound && valuesAfterOptions) {
                            throw new InvalidOptionsException(
                                "Options must appear before non-option arguments."
                            );
                        }
                        ParseLongOption(args, longOpts, ref i, arg, parsedOpts);
                        continue;
                    }
                    if (arg.StartsWith("-")) {
                        if (valueFound && valuesAfterOptions) {
                            throw new InvalidOptionsException(
                                "Options must appear before non-option arguments."
                            );
                        }
                        ParseShortOptions(args, shortOpts, ref i, arg, parsedOpts);
                        continue;
                    }
                }
                if (valuesAfterOptions) {
                    parsingOptions = false;
                    valueFound = true;
                }
                retVals.Add(arg);
            }
            if (!ignoreRequiredOnHelpAndVersion ||
                (
                    !parsedOpts.ContainsKey(singleDashLongOptions ? "-help" : "--help") &&
                    !parsedOpts.ContainsKey(singleDashLongOptions ? "-version" : "--version")
                )
            ) {
                foreach (var opt in requiredOpts) {
                    if (!opt.Found) {
                        throw new InvalidOptionsException(
                            $"Required option ({opt.FullFirstName}) is missing."
                        );
                    }
                }
            }
            var retOpts = ProcessOptionValues(parsers, parsedOpts);
            if (includeUnusedOptions) {
                foreach (var opt in allOpts) {
                    if (!retOpts.ContainsKey(opt.FirstName)) {
                        retOpts.Add(opt.FirstName, null);
                    }
                }
            }
            return new ParsedArgs(retOpts, retVals.ToArray());
        }

        /// <summary>
        /// Parses program arguments.
        /// </summary>
        /// <param name="args">Program arguments to process.</param>
        /// <param name="singleDashLongOptions">
        /// If true, all options are long options and are prefixed with a single hyphen.<br/>
        /// If false, short options are prefixed with a single hyphen and can be used in groups,
        /// long options are prefixed with two hyphens.<br/>
        /// Default: false
        /// </param>
        /// <param name="valuesAfterOptions">
        /// If true, non-option arguments must appear after the last option argument.<br/>
        /// Default: false
        /// </param>
        /// <returns>
        /// A <see cref="ParseOptions"/> object with options and values extracted from
        /// <paramref name="args"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="args"/> is null.
        /// </exception>
        /// <exception cref="InvalidOptionsException">
        /// Thrown when <paramref name="args"/> contains invalid options
        /// </exception>
        /// <para><b>Remarks</b></para>
        /// <remarks>
        /// No option can be repeated, option values can only be specified in the form -o=value (or
        /// --option=value).
        /// </remarks>
        /// <seealso cref="Parse(string[],string,bool,bool,bool,bool,bool,System.Collections.Generic.Dictionary{string,System.Func{string,string,object?}}?)"/>
        /// <seealso cref="ParsedArgs"/>
        public static ParsedArgs Parse(
            string[] args,
            bool singleDashLongOptions = false,
            bool valuesAfterOptions = false
        ) {
            if (args is null) {
                throw new ArgumentNullException(nameof(args));
            }
            if (args.Length == 0) {
                return new ParsedArgs(new Dictionary<string, object?>(), Array.Empty<string>());
            }
            var retVals = new List<string>();
            var retOpts = new Dictionary<string, object?>();
            var parsingOptions = true;
            var valueFound = false;
            for (var i = 0; i < args.Length; ++i) {
                var arg = args[i];
                if (!parsingOptions) {
                    retVals.Add(arg);
                    continue;
                }
                if (!arg.StartsWith("-") || arg.Length < 2) {
                    if (valuesAfterOptions) {
                        parsingOptions = false;
                        valueFound = true;
                    }
                    retVals.Add(arg);
                    continue;
                }
                if (arg == "--") {
                    if (valueFound && valuesAfterOptions) {
                        retVals.Add(arg);
                    }
                    parsingOptions = false;
                    continue;
                }
                if (singleDashLongOptions) {
                    if (arg.StartsWith("--")) {
                        throw new InvalidOptionsException($"Invalid option '{arg}'.");
                    }
                    if (arg.StartsWith("-")) {
                        if (valueFound && valuesAfterOptions) {
                            throw new InvalidOptionsException(
                                "Options must appear before non-option arguments."
                            );
                        }
                        ParseAnyLongOption(arg, retOpts);
                        continue;
                    }
                } else {
                    if (arg.StartsWith("---")) {
                        throw new InvalidOptionsException($"Invalid option '{arg}'.");
                    }
                    if (arg.StartsWith("--")) {
                        if (valueFound && valuesAfterOptions) {
                            throw new InvalidOptionsException(
                                "Options must appear before non-option arguments."
                            );
                        }
                        ParseAnyLongOption(arg, retOpts);
                        continue;
                    }
                    if (arg.StartsWith("-")) {
                        if (valueFound && valuesAfterOptions) {
                            throw new InvalidOptionsException(
                                "Options must appear before non-option arguments."
                            );
                        }
                        ParseAnyShortOptions(arg, retOpts);
                        continue;
                    }
                }
                if (valuesAfterOptions) {
                    parsingOptions = false;
                    valueFound = true;
                }
                retVals.Add(arg);
            }
            return new ParsedArgs(retOpts, retVals.ToArray());
        }

        private static Dictionary<string, object?> ProcessOptionValues(
            Dictionary<string, Parser>? parsers,
            Dictionary<string, Option> parsedOpts
        ) {
            var retOpts = new Dictionary<string, object?>();
            foreach (var parsedOpt in parsedOpts) {
                var opt = parsedOpt.Value;
                Parser? parser = null;
                retOpts.Add(
                    parsedOpt.Key,
                    parsers?.TryGetValue(opt.FirstName, out parser) == true
                        ? opt.ParseValue(parser)
                        : opt.ParseValue()
                );
            }
            return retOpts;
        }

        private static void ParseShortOptions(
            string[] args,
            Dictionary<char, Option> shortOpts,
            ref int i,
            string arg,
            Dictionary<string, Option> parsedOpts
        ) {
            var match = shortOptionRegex.Match(arg);
            if (!match.Success) {
                throw new InvalidOptionsException($"Invalid option '{arg}'.");
            }
            foreach (var o in match.Groups["noValueNames"].Value) {
                if (!shortOpts.TryGetValue(o, out var opt)) {
                    throw new InvalidOptionsException($"Unknown option '-{o}'.");
                }
                if (opt.HasValue) {
                    throw new InvalidOptionsException(
                        $"Option '{match.Groups["fullName"].Value}' requires a value."
                    );
                }
                opt.Find("-" + o);
                parsedOpts[opt.FirstName] = opt;
            }
            var lastName = match.Groups["lastName"].Value[0];
            if (!shortOpts.TryGetValue(lastName, out var opt2)) {
                throw new InvalidOptionsException(
                    $"Unknown option '-{lastName}'."
                );
            }
            FindOption(args, ref i, "-" + lastName, match.Groups["value"], opt2);
            parsedOpts[opt2.FirstName] = opt2;
        }

        private static void ParseAnyShortOptions(string arg, Dictionary<string, object?> retOpts) {
            var match = shortOptionRegex.Match(arg);
            if (!match.Success) {
                throw new InvalidOptionsException($"Invalid option '{arg}'.");
            }
            foreach (var o in match.Groups["noValueNames"].Value) {
                retOpts.Add(o.ToString(), true);
            }
            var lastName = match.Groups["lastName"].Value;
            var valueGroup = match.Groups["value"];
            retOpts.Add(lastName, valueGroup.Success ? valueGroup.Value : (object)true);
        }

        private static void ParseLongOption(
            string[] args,
            Dictionary<string, Option> longOpts,
            ref int i,
            string arg,
            Dictionary<string, Option> parsedOpts
        ) {
            var match = longOptionRegex.Match(arg);
            if (!match.Success) {
                throw new InvalidOptionsException($"Invalid option '{arg}'.");
            }
            if (!longOpts.TryGetValue(match.Groups["name"].Value, out var opt)) {
                throw new InvalidOptionsException(
                    $"Unknown option '{match.Groups["fullName"].Value}'."
                );
            }
            FindOption(args, ref i, match.Groups["fullName"].Value, match.Groups["value"], opt);
            parsedOpts[opt.FirstName] = opt;
        }

        private static void ParseAnyLongOption(string arg, Dictionary<string, object?> retOpts) {
            var match = longOptionRegex.Match(arg);
            if (!match.Success) {
                throw new InvalidOptionsException($"Invalid option '{arg}'.");
            }
            var valueGroup = match.Groups["value"];
            retOpts.Add(
                match.Groups["name"].Value,
                valueGroup.Success ? valueGroup.Value : (object)true
            );
        }

        private static void FindOption(
            string[] args,
            ref int i,
            string optName,
            Group valueGroup,
            Option opt
        ) {
            if (opt.HasValue) {
                if (valueGroup.Success) {
                    opt.Find(optName, valueGroup.Value);
                } else if (i < args.Length - 1) {
                    ++i;
                    opt.Find(optName, args[i]);
                } else {
                    throw new InvalidOptionsException(
                        $"Option '{optName}' requires a value."
                    );
                }
            } else if (valueGroup.Success) {
                throw new InvalidOptionsException(
                    $"Option '{optName}' can't have a value."
                );
            } else {
                opt.Find(optName);
            }
        }

        private static Option.OptionValueType GetValueType(string modifier) {
            var index = modifier.IndexOf(':');
            if (index < 0) {
                return Option.OptionValueType.None;
            } else if (index + 1 < modifier.Length) {
                return modifier[index + 1] switch {
                    'i' => Option.OptionValueType.Int,
                    'n' => Option.OptionValueType.Double,
                    'f' => Option.OptionValueType.File,
                    'd' => Option.OptionValueType.Directory,
                    's' => Option.OptionValueType.String,
                    't' => Option.OptionValueType.DateTime,
                    // default type
                    _ => Option.OptionValueType.String
                };
            } else {
                // default type
                return Option.OptionValueType.String;
            }
        }

        private static (
            List<Option> allOpts,
            List<Option> requiredOptions,
            Dictionary<char, Option> shortOptions,
            Dictionary<string, Option> longOptions
        ) ParseOptions(
            string options,
            bool singleDashLongOptions,
            bool autoAddHelpAndVersionOptions
        ) {
            var allOptions = new List<Option>();
            var requiredOptions = new List<Option>();
            var shortOptions = new Dictionary<char, Option>();
            var longOptions = new Dictionary<string, Option>();
            foreach (var opt in options.Split(',')) {
                Option? option = null;
                var match = optionsDefinitionRegex.Match(opt);
                if (!match.Success) {
                    throw new FormatException("Options are not in a valid format.");
                }
                var modifier = match.Groups["modifier"].Value;
                foreach (var variant in opt.TrimEnd(':', '+', '!').Split('|')) {
                    if (singleDashLongOptions) {
                        var fullName = '-' + variant;
                        if (longOptions.ContainsKey(variant)) {
                            throw new FormatException(
                                $"Duplicate option definition ({fullName})"
                            );
                        }
                        option ??= new Option(
                            fullName,
                            modifier.Contains('+'),
                            GetValueType(modifier)
                        );
                        longOptions.Add(variant, option);
                    } else {
                        if (variant.Length == 1) {
                            if (shortOptions.ContainsKey(variant[0])) {
                                throw new FormatException(
                                    $"Duplicate short option definition (-{variant[0]})"
                                );
                            }
                            option ??= new Option(
                                '-' + variant,
                                modifier.Contains('+'),
                                GetValueType(modifier)
                            );
                            shortOptions.Add(variant[0], option);
                        } else {
                            var fullName = "--" + variant;
                            if (longOptions.ContainsKey(variant)) {
                                throw new FormatException(
                                    $"Duplicate long option definition ({fullName})"
                                );
                            }
                            option ??= new Option(
                                fullName,
                                modifier.Contains('+'),
                                GetValueType(modifier)
                            );
                            longOptions.Add(variant, option);
                        }
                    }
                }
                if (option is null) {
                    // should not be possible thanks to regex check
                    throw new FormatException();
                }
                if (modifier.Contains('!')) {
                    requiredOptions.Add(option);
                }
                allOptions.Add(option);
            }
            if (autoAddHelpAndVersionOptions) {
                if (!longOptions.ContainsKey("help")) {
                    longOptions.Add(
                        "help",
                        new Option(
                            singleDashLongOptions ? "-help" : "--help",
                            false,
                            Option.OptionValueType.None
                        )
                    );
                }
                if (!longOptions.ContainsKey("version")) {
                    longOptions.Add(
                        "version",
                        new Option(
                            singleDashLongOptions ? "-version" : "--version",
                            false,
                            Option.OptionValueType.None
                        )
                    );
                }
            }
            return (allOptions, requiredOptions, shortOptions, longOptions);
        }
    }
}
