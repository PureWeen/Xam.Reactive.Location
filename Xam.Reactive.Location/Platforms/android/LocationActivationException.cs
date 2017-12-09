using Android.Gms.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Xam.Reactive.Location
{

    public partial class LocationActivationException : Exception
    {
        public ConnectionResult ResultCode { get; }
        public LocationActivationException(ActivationFailedReasons reason, ConnectionResult resultCode) :
            this(reason)
        {
            ResultCode = resultCode;
        }
    }
}
