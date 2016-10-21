using System;
using System.Collections.Generic;

namespace WeatherUtility.Models
{

    public class Station
    {

        public Station( )
        {
            StationProperties = new Dictionary<string, string>();
        }

        public Guid DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string Address { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public double CurrentRain { get; set; }
        public string CoolingBaseTemperatureC { get; set; }
        public string HeatingBaseTemperatureC { get; set; }

        public Dictionary<string, string> StationProperties;

    }

}