using Switch.Connections;

namespace WeatherUtility.Utilities
{

    public static class Invariables
    {

        //public static string blobContainer = "data-exchange";

        public static string readingsTableName = "ObservationStaging";

        //public static string readingsBlob = "timeseries-test";

        //public static string weatherDataBlob = "weather-data";

        //public static string driverDeviceType = "WeatherStation";

        //public static string driverClassName = "WWO Weather Station";

        public static string weatherHistory = "airport-weather-history";

        public static string weatherHistoryTableName = "WeatherHistoryDownload";

        public static string partitionKey = "c0e614b9-d5fc-4107-a9ed-863c4730e23e";

        public static string wwoInstallation = "8524f7e6-7b72-45a8-90c2-fe4d99530713"; // World Weather Online Feed (Live)

        //public static string weatherStationDeviceId = "4a4fc263-dbd7-4e78-ac52-6d5efa5e0ddf"; // WWO Autotag Test 2 - WeatherStationDeviceID

        public static readonly string SwitchStorageConnection = ConnectionStringHelper.Get( "SwitchStorage" );  //"switchstorage";  //"H5mpP6dFJ8QkfKEYy7aIXUjuHfwBzWSp32dOPQQYtxR1eEy3gZ1BFuXvj8B/FDjJo3VKiCiXpuQJX69ZBdaIKg==";

        //public static readonly string switchContainerConnection = ConnectionStringHelper.Get( "SwitchContainer" );  //"switchcontainer";   //"2NaOVuS/zi0LV3WpdtmvXqJuoetuCdLaKYBT7PkmYM7T23ZVQegWzcnK9En9uXXNhDnF4F8seI2NRZjyyJBslA==";

        public static readonly string SwitchConnection = ConnectionStringHelper.Get( "Switch" );

    }

}