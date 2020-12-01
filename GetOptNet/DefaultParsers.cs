using System;
using System.Globalization;
using System.IO;

namespace GetOptNet {
    
    internal class DefaultParsers {

        public static object IntParser(string optionName, string value) {
            if (int.TryParse(
                    value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var i
                )
            ) {
                return i;
            } else {
                throw new InvalidOptionsException(
                    $"Option '{optionName}' expects an integer as its value."
                );
            }
        }

        public static object DoubleParser(string optionName, string value) {
            if (double.TryParse(
                    value,
                    NumberStyles.Float | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture,
                    out var d
                )
            ) {
                return d;
            } else {
                throw new InvalidOptionsException(
                    $"Option '{optionName}' expects a decimal number as its value."
                );
            }
        }

        public static object FileInfoParser(string optionName, string value) {
            try {
                return new FileInfo(value);
            } catch {
                throw new InvalidOptionsException(
                    $"Option '{optionName}' expects a file path as its value."
                );
            }
        }

        public static object DirectoryInfoParser(string optionName, string value) {
            try {
                return new DirectoryInfo(value);
            } catch {
                throw new InvalidOptionsException(
                    $"Option '{optionName}' expects a file path as its value."
                );
            }
        }

        public static object DateTimeParser(string optionName, string value) {
            if (DateTime.TryParse(value, out var dt)) {
                return dt;
            } else {
                throw new InvalidOptionsException(
                    $"Option '{optionName}' expects a date as its value."
                );
            }
        }
    }
}
