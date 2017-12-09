using System;
using System.Collections.Generic;
using System.Text;

namespace Xam.Reactive.Location
{

    public enum ActivationFailedReasons
    {
        Unknown = 0,
        GooglePlayServicesNotAvailable = 1
    }

    public partial class LocationActivationException : Exception
    {
        public ActivationFailedReasons Reason { get; }
        public LocationActivationException(ActivationFailedReasons reason)
        {
            Reason = reason;
        }
    }
}
