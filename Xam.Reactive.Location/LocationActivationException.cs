using System;
using System.Collections.Generic;
using System.Text;

namespace Xam.Reactive.Location
{

    public enum ActivationFailedReasons
    {
        Unknown = 0,
        GooglePlayServicesNotAvailable = 1,
        PermissionsIssue = 2
    }

    public partial class LocationActivationException : Exception
    {
        public ActivationFailedReasons Reason { get; }
        public LocationActivationException(ActivationFailedReasons reason)
            : base($"{reason}")
        {
            Reason = reason;
        }

        public LocationActivationException(ActivationFailedReasons reason, Exception innerException)
            : base($"{reason}", innerException)
        {
            Reason = reason;
        }



        public LocationActivationException(ActivationFailedReasons reason, string message)
            : base(message)
        {
            Reason = reason;
        }
    }
}
