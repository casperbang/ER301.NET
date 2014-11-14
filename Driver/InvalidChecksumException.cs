using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BangBits.ER301.Driver
{
    class InvalidChecksumException : Exception
    {
        public InvalidChecksumException(string message) : base(message)
        {
        }
    }
}
