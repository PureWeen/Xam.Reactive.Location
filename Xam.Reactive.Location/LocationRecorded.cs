﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xam.Reactive.Location
{
    public class LocationRecorded
    {
        private double latitude;
        private double longitude;
        private double altitude;
        private double speed;
        private DateTimeOffset recorded;
        private double accuracy;
        private double? altitudeAccuracy;

        public LocationRecorded() { }
        public LocationRecorded(double latitude, double longitude, DateTimeOffset recorded, float accuracy)
        {
            this.latitude = latitude;
            this.longitude = longitude;
            this.recorded = recorded;
            this.accuracy = accuracy;
        }

        public double Latitude { get => latitude; protected set => latitude = value; }
        public double Longitude { get => longitude; protected set => longitude = value; }
        public double Altitude { get => altitude; protected set => altitude = value; }
        public double Speed { get => speed; protected set => speed = value; }
        public DateTimeOffset Recorded { get => recorded; protected set => recorded = value; }
        public double Accuracy { get => accuracy; set => accuracy = value; }
        public double? AltitudeAccuracy { get => altitudeAccuracy; protected set => altitudeAccuracy = value; }
    }
}
