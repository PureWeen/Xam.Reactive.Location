using System;
using System.Collections.Generic;
using System.Text;

namespace Xam.Reactive.Location
{
    public class LocationListeningException : Exception
    {
        public LocationListeningException()
        {
        }

        public LocationListeningException(string message)
            : base(message)
        {
        }

        public LocationListeningException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
