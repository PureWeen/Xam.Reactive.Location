using Android.Gms.Common;
using Android.Gms.Common.Apis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Xam.Reactive.Location
{

    public class LocationActivationException : Exception
    {
        public ApiException ApiException { get; }
        public LocationActivationException(
            ActivationFailedReasons reason, 
            ApiException apiException) :
            this(reason, apiException.StatusMessage)
        {
            ApiException = apiException;
        }
    }
}
