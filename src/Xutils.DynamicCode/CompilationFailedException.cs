using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Xutils.DynamicCode
{
    [Serializable]
    internal class CompilationFailedException : Exception
    {
        public string[] Errors { get; }

        public CompilationFailedException(IEnumerable<string> errors)
        {
            Errors = errors.ToArray();
        }

        public CompilationFailedException(string message, IEnumerable<string> errors = null) : base(message)
        {
            Errors = errors.ToArray();
        }

        public CompilationFailedException(string message, Exception innerException, IEnumerable<string> errors = null) : base(message, innerException)
        {
            Errors = errors.ToArray();
        }

        protected CompilationFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}