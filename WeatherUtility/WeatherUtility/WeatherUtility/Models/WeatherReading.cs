using System;

namespace WeatherUtility.Models
{

    public class WeatherReading
    {

        public string RowKey { get; set; }
        public DateTime DateTime { get; set; }
        public double Value { get; set; }

    }

}