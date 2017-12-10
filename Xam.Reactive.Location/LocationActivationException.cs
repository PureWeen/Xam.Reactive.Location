using System;
using System.Collections.Generic;
using System.Text;

namespace Xam.Reactive.Location
{

    public enum ActivationFailedReasons
    {
        // todo work on reasons
        CheckExceptionOnPlatform = 0,
        LocationServicesNotAvailable = 1,
        PermissionsIssue = 2,
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
