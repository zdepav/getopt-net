#nullable enable
using System.Linq;
using OptionTuple = System.ValueTuple<string, object?>;
using Options = System.Collections.Generic.IDictionary<string, object?>;

namespace GetOptNet {

    public static partial class GetOpt {

        /// <summary>
        /// Container for options and values extracted by <see cref="GetOpt.Parse"/>.
        /// </summary>
        public readonly struct ParsedArgs {

            /// <summary>
            /// Dictionary mapping option names to values. For details about value types see
            /// <see cref="GetOpt.Parse"/>.
            /// </summary>
            public readonly Options Options;

            /// <summary>
            /// Array of non-option arguments.
            /// </summary>
            public readonly string[] Values;

            internal ParsedArgs(Options options, string[] values) {
                Options = options;
                Values = values;
            }

            internal ParsedArgs(OptionTuple[] options, string[] values) {
                Options = options.ToDictionary(o => o.Item1, o => o.Item2);
                Values = values;
            }
        }
    }
}
