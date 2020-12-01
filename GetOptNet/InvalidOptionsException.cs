#nullable enable
using System;

namespace GetOptNet {
    
    public class InvalidOptionsException : Exception {

        public InvalidOptionsException(string message) : base(message) { }

        public InvalidOptionsException(string message, Exception cause) : base(message, cause) { }
    }
}
