using System;

namespace WeatherUtility.Models
{

    public class WeatherStationSensorCached
    {

        public Guid ObjectPropertyid { get; set; }
        public string PropertyName { get; set; }
        public string PropertyValue { get; set; }
        public Guid DeviceID { get; set; }
        public bool IsConfiguration { get; set; }
        public string DeviceName { get; set; }

    }

    public class WeatherStationsCached
    {

        public Guid DeviceID { get; set; }
        public string DeviceName { get; set; }

    }

}