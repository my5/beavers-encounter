using System;
using System.Runtime.Serialization;

namespace Beavers.Encounter.ApplicationServices
{
    public class MaxCodesCountException : Exception
    {
        public MaxCodesCountException()
            : base()
        {
        }

        public MaxCodesCountException(string message)
            : base(message)
        {
        }

        protected MaxCodesCountException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public MaxCodesCountException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
