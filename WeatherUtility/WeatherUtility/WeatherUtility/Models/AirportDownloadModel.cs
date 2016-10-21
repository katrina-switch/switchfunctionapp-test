using System;

namespace WeatherUtility.Models
{

    public class AirportDownloadModel
    {

        public string AirportCode { get; set; }
        public bool Downloaded { get; set; }
        public string WeatherStationDeviceId { get; set; }
        public DateTime StartDownload { get; set; }
        public DateTime EndDownload { get; set; }
        // last uploaded file
        public DateTime LastDownload { get; set; }

    }

}