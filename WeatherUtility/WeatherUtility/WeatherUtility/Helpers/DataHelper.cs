using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Microsoft.WindowsAzure.Storage;
using Switch.Blob;
using Switch.Json.Linq;
using Switch.Redis;
using Switch.Sql;
using Switch.Table;
using WeatherUtility.Models;
using WeatherUtility.Utilities;
using Switch.Time;

namespace WeatherUtility.Helpers
{

    public static class DataHelper
    {

        #region ' Global Variables '

        private static string errormessage = "";
        private static bool _isProcessing;
        
        private static readonly Queue<PostingData> _postingQueue;

        private static List<Airport> _airports { get; set; }

        #endregion

        static DataHelper()
        {
            _postingQueue = new Queue<PostingData>();
        }

        #region ' LOAD ALL '

        private static List<Airport> LoadAirportData( out string errmsg )
        {
            var errors = new StringBuilder( );
            try
            {
                if ( _airports != null && _airports.Count > 0 )
                {
                    errmsg = "";
                    return _airports;
                }

                var key = "get-airport-data";
                var obj = CacheHelper.GetCachedItem( key );
                if ( obj == null )
                {
                    var arr = BlobHelper.GetBlob( Invariables.SwitchStorageConnection, "airportdata.csv", "system-defaults" );
                    var lines = Encoding.ASCII.GetString( arr ).Split( '\n' );
                    var i = 0;
                    foreach ( var line in lines )
                    {
                        try
                        {
                            if ( i != 0 )
                            {
                                var tokens = StringSplit( line.Replace( "\r", "" ) );
                                if ( tokens.Length > 0 )
                                {
                                    if ( _airports == null )
                                        _airports = new List<Airport>( );

                                    _airports.Add( new Airport
                                    {
                                        Code = tokens[0],
                                        AirportName = tokens[1],
                                        Latitude = double.Parse( tokens[2] ),
                                        Longitude = double.Parse( tokens[3] ),
                                        Area = tokens[4]
                                    } );
                                }
                            }
                        }
                        catch ( Exception )
                        {
                            errors.AppendLine( "Error line # " + i + Environment.NewLine );
                            Logger.ErrorLog( "WeatherUtility", "LoadAirportData", "Error line # " + i );
                        }
                        finally
                        {
                            i++;
                        }
                    }

                    if (_airports != null && _airports.Count > 0)
                        CacheHelper.AddToCache(key, _airports);

                    if (_airports != null)
                    {
                        Logger.LocalLog( "WeatherUtility", "LoadAirportData", "LogMessage", "Loaded " + _airports.Count + " airports" );

                        errmsg = errors.Length > 5 ? errors.ToString() : "";
                        return _airports;
                    }

                    errmsg = "";
                    return _airports;
                }
                errmsg = "";
                _airports = ( List<Airport> )obj;
                return _airports;
            }
            catch ( Exception exception )
            {
                errmsg = "Error: " + exception;
                Logger.ErrorLog( "DataHelper", "LoadAirportData", exception.ToString( ) );
                return null;
            }
        }

        #endregion

        #region ' GET BY LIST '

        static IEnumerable<FileBlock> GetFileBlocks( Stream fs )
        {
            var hashSet = new HashSet<FileBlock>( );
            int blockId = 0;
            var buf = new byte[250000];
            int bytesRead;

            while ( ( bytesRead = fs.Read( buf, 0, buf.Length ) ) > 0 )
            {
                var chunk = new byte[bytesRead];
                Array.Copy( buf, chunk, chunk.Length );
                hashSet.Add( new FileBlock { Content = chunk, Id = Convert.ToBase64String( Encoding.ASCII.GetBytes( blockId.ToString( ).PadLeft( 7, '0' ) ) ) } );
                blockId++;
            }

            return hashSet;

        }

        public static Tuple<string, string> GetApiProjectKeys(string installation)
        {
            try
            {
                string key = $"getprojectidapikeysbyinstallationid_{installation}";
                var obj = CacheHelper.GetCachedItem( key );
                if ( obj != null )
                {
                    return ( Tuple<string, string> )obj;
                }

                var sql = @"SELECT B.ProjectIdentifier, A.ApiKey 
                            FROM ApiKeys A INNER JOIN Apiprojects B ON A.ApiProjectID = B.ProjectID  
                            WHERE A.installationID = @installation ";

                int retry = 0;
                bool ok = false;
                DataTable dt = null;

                while ( !ok && retry < 3 )
                {
                    try
                    {
                        retry++;
                        Thread.Sleep( 5 );
                        dt = SqlHelper.ExecuteReader( Invariables.SwitchConnection, sql,
                            new List<SqlParam> { new SqlParam("installation", Guid.Parse(installation), SqlDbType.UniqueIdentifier)} );
                        ok = true;
                    }
                    catch ( Exception ex)
                    {
                        Debug.WriteLine( ex.ToString( ) );
                    }
                }

                if ( dt != null && dt.Rows.Count > 0 )
                {
                    var apiKey = dt.Rows[0]["ApiKey"].ToString( );
                    var projId = dt.Rows[0]["ProjectIdentifier"].ToString( );

                    CacheHelper.AddToCache( key, new Tuple<string, string>( apiKey, projId ) );
                    return new Tuple<string, string>( apiKey, projId );
                }
            }
            catch ( Exception exception )
            {
                Logger.ErrorLog( "DataHelper", "GetApiProjectKeys", exception.ToString( ) );
            }
            return new Tuple<string, string>( "", "" );
        }

        #endregion

        #region ' GET BY DATATYPE '

        public static string GeneratePayloadEntry( StringBuilder currentPaylod, string propertyId, string timeUtc, string value )
        {
            var template = new StringBuilder( @"{""Id"":""$$1"",""TypeId"":""1"",""Type"":""Device"",""Time"":""$$2"",""Name"":"""",""Val"":""$$3"",""Raw"":""$$3"",""STime"":""$$2"",""SubValues"" : """"}" );
            try
            {   
                template = template.Replace( "$$1", propertyId ).Replace( "$$2", timeUtc ).Replace( "$$3", value );

                if ( currentPaylod.ToString( ) != "[" )
                    template.Insert( 0, "," );
            }
            catch ( Exception exception )
            {
                Debug.WriteLine( exception.Message );
                Logger.ErrorLog( "DataHelper", "GeneratePayloadEntry", exception.ToString( ) );
            }
            return template.ToString( );
        }

        public static string GetHistory( string weatherStationDeviceId )
        {
            try
            {
                var airportCode = RetrieveNearestAirport( weatherStationDeviceId );
                Logger.LocalLog( "WeatherUtility", "GetHistory", "LogMessage", "Adding to downloadlist " + airportCode );

                if ( !string.IsNullOrWhiteSpace( airportCode ) )
                {
                    var obj = new AirportDownloadModel
                    {
                        AirportCode = airportCode,
                        Downloaded = false,
                        WeatherStationDeviceId = weatherStationDeviceId,

                        EndDownload = GetEarliestRecord( weatherStationDeviceId ),
                        LastDownload = new DateTime( DateTime.Now.Year - 1, 1, 1 ),
                        StartDownload = new DateTime( DateTime.Now.Year - 1, 1, 1 )
                    };
                    TableStorageRestHelper.InsertOrReplaceEntity( Invariables.SwitchStorageConnection, Invariables.weatherHistoryTableName, Invariables.partitionKey, airportCode, obj );
                }
                return airportCode;
            }
            catch ( Exception exception )
            {
                Debug.WriteLine( exception.Message );
                Logger.ErrorLog( "DataHelper", "GetHistory", exception.ToString( ) );
                return null;
            }
        }

        public static string[] StringSplit( string s )
        {
            List<string> lst = new List<string>();
            string[] tokens = null;
            try
            {
                var skip = false;
                var delimiterFound = false;
                var sb = new StringBuilder( );
                foreach ( var c in s.ToCharArray( ) )
                {
                    if ( skip )
                        skip = false;
                    else
                    {
                        if ( c == '"' )
                        {
                            if ( delimiterFound ) // found the end
                            {
                                lst.Add( sb.ToString( ).Trim( ) );
                                delimiterFound = false;
                                sb.Clear( );
                                skip = true;
                            }
                            else // found the beginning
                                delimiterFound = true;
                        }
                        else
                        {
                            if ( c == ',' )
                            {
                                if ( !delimiterFound )
                                {
                                    lst.Add( sb.ToString( ).Trim( ) );
                                    sb.Clear( );
                                }
                                else
                                    sb.Append( c );
                            }
                            else
                                sb.Append( c );
                        }
                    }
                }

                if ( sb.ToString( ) != "" )
                    lst.Add( sb.ToString( ).Trim( ) );

                tokens = new string[lst.Count];
                for ( int i = 0; i < tokens.Length; i++ )
                    tokens[i] = lst[i];
            }
            catch ( Exception exception )
            {
                Logger.ErrorLog( "DataHelper", "StringSplit", exception.ToString( ) );
            }
            return tokens;
        }

        public static double CelciusToF( double c )
        {
            return Math.Round( c * 1.8 + 32, 2 );
        }

        public static double FahrenheitToC( double f )
        {
            return Math.Round( ( f - 32 ) * ( 5.0 / 9.0 ), 2 );
        }

        public static double ComputeWetBulbTemperature( double drybulb, double rh )
        {
            //drybulb = 45;
            //rh = 60;
            var wbt = double.NaN;
            try
            {
                var x1 = 273.16 / ( ( drybulb - 32 ) / 1.8 + 273.16 );
                var x2 = -10.7959 * ( 1 - x1 ) - ( 2.1836 * Math.Log( x1 ) ) + 2.2196;
                var x3 = 29.92 * ( 1 / Math.Exp( 2.3026 * x2 ) );
                var pvp = rh * x3 / 100;

                if ( drybulb < 40 )
                {

                    var nn = pvp * 0.491154;
                    var y = Math.Log( nn );
                    double dp = 0d;
                    if ( pvp < 0.18036 )
                        dp = 90.12 + ( 26.142 * y ) + ( 0.8927 * y * y );
                    else
                    {
                        dp = 100.45 + ( 33.193 * y ) + ( 2.319 * y * y );
                        dp = dp + 0.17074 * ( Math.Pow( y, 3 ) ) + 1.2063 * Math.Pow( pvp * 0.491154, 0.1984 );
                    }

                    wbt = drybulb - ( drybulb - dp ) / 3;
                }
                else
                {
                    var x4 = 0.622 * pvp / ( 29.92 - pvp );
                    var enthalpy = ( 0.24 + ( 0.444 * x4 ) ) * drybulb + ( 1061 * x4 );
                    wbt = -0.015991 * enthalpy * enthalpy + 2.374 * enthalpy + 7.5089;
                }
            }
            catch ( Exception exception )
            {
                Logger.ErrorLog( "DataHelper", "ComputeWetBulbTemperature", exception.ToString( ) );
            }

            if ( double.IsNaN( wbt ) )
                return wbt;
            return Math.Round( wbt, 2 );
        }

        public static List<Guid> GetFirstSensor( Guid weatherStationId, out string errmsg )
        {
            List<Guid> sensorList = new List<Guid>();
            try
            {
                var key = "get-weather-station-device-sensor-" + weatherStationId;
                var obj = CacheHelper.GetCachedItem( key );
                if ( obj == null )
                {
                    int retry = 0;
                    bool ok = false;
                    DataTable dt = null;

                    var sql = @"SELECT A.ObjectPropertyID  
                                FROM ObjectProperties A INNER JOIN  
		                                DeviceSpecs B ON B.DeviceID = A.DeviceID  
                                WHERE B.DriverDeviceType='WeatherStation'  
		                                AND A.DeviceID = @deviceid  ";

                    while ( !ok && retry < 3 )
                        try
                        {
                            retry++;
                            Thread.Sleep( 5 );
                            dt = SqlHelper.ExecuteReader( Invariables.SwitchConnection, sql,
                                    new List<SqlParam> { new SqlParam("deviceid", weatherStationId, SqlDbType.UniqueIdentifier) } );
                            ok = true;
                        }
                        catch ( Exception )
                        {
                        }

                    if ( dt != null && dt.Rows.Count > 0 )
                    {
                        foreach (DataRow drow in dt.Rows)
                        {
                            if (!string.IsNullOrEmpty(drow[0].ToString()))
                            {
                                if ( !sensorList.Contains( Guid.Parse( drow[0].ToString( ) ) ) )
                                    sensorList.Add(Guid.Parse(drow[0].ToString()));
                            }
                        }
                    }

                    errmsg = "";
                    CacheHelper.AddToCache( key, sensorList );
                    return sensorList;
                }
                errmsg = "";
                return ( List<Guid> )obj;
            }
            catch ( Exception exception )
            {
                errmsg = "Error: " + exception;
                Logger.ErrorLog( "DataHelper", "GetFirstSensor", exception.ToString( ) );
                return sensorList;
            }
        }

        #endregion

        #region ' GET BY CLASS "

        public static Station GetStation( string weatherDeviceId )
        {
            try
            {
                var cachedkey = $"get_weather_station_sensor_by_deviceid_{weatherDeviceId}";
                var cachedobj = CacheHelper.GetCachedItem( cachedkey );
                if ( cachedobj == null )
                {
                    var retry = 0;

                    List<WeatherStationSensorCached> wsts = null;
                    do
                    {
                        if ( retry == 2 )
                            RedisHelper.Instance.Delete( "GetWeatherSensorsPerDevice" );

                        wsts = Switch.Cache.CacheHelper.GetList<WeatherStationSensorCached>( "GetWeatherSensorsPerDevice", new[] { weatherDeviceId } );
                        if ( wsts == null )
                            retry++;

                    } while ( wsts == null && retry < 3 );

                    if ( wsts != null && wsts[0].DeviceID != Guid.Empty )
                    {
                        var st = new Station
                        {
                            DeviceId = wsts[0].DeviceID,
                            DeviceName = wsts[0].DeviceName
                        };

                        // retrieve the address for this station
                        var dtAdr = SqlHelper.ExecuteReader( "select address from devicespecs where deviceid=@deviceId",
                                  new List<SqlParam> {
                                      new SqlParam("deviceId", st.DeviceId, SqlDbType.UniqueIdentifier)
                                  } );
                        if ( dtAdr != null && dtAdr.Rows.Count > 0 )
                            st.Address = dtAdr.Rows[0][0].ToString( );

                        foreach ( WeatherStationSensorCached cache in wsts )
                        {
                            switch (cache.PropertyName.ToLower())
                            {
                                case "latitude":
                                    st.Latitude = Convert.ToDouble( cache.PropertyValue );
                                    break;
                                case "longitude":
                                    st.Longitude = Convert.ToDouble( cache.PropertyValue );
                                    break;
                                case "raincurrent":
                                    if ( !st.StationProperties.ContainsKey( cache.PropertyName ) )
                                        st.StationProperties.Add( cache.PropertyName, cache.ObjectPropertyid.ToString( ) );

                                    var currentRain = 0d;
                                    double.TryParse( cache.PropertyValue, out currentRain );
                                    st.CurrentRain = currentRain;
                                    break;
                                case "heatingbasetemperaturec":
                                    if ( !st.StationProperties.ContainsKey( cache.PropertyName ) )
                                        st.StationProperties.Add( cache.PropertyName, cache.ObjectPropertyid.ToString( ) );

                                    st.HeatingBaseTemperatureC = cache.PropertyValue.Trim( );
                                    break;
                                case "coolingbasetemperaturec":
                                    if ( !st.StationProperties.ContainsKey( cache.PropertyName ) )
                                        st.StationProperties.Add( cache.PropertyName, cache.ObjectPropertyid.ToString( ) );

                                    st.CoolingBaseTemperatureC = cache.PropertyValue.Trim( );
                                    break;
                                default:
                                    if ( !st.StationProperties.ContainsKey( cache.PropertyName ) )
                                        st.StationProperties.Add( cache.PropertyName, cache.ObjectPropertyid.ToString( ) );
                                    break;
                            }
                        }

                        CacheHelper.AddToCache( cachedkey, st );
                        return st;
                    }
                }
                else
                    return ( Station )cachedobj;
            }
            catch ( Exception exception )
            {
                Logger.ErrorLog( "DataHelper", "GetStation", exception.ToString( ) );
            }
            return null;
        }

        #endregion

        #region ' Upload / Post Weather Data '

        public static void PostObservationEnqueue( string projectId, string installationId, string payload, string apikey )
        {
            try
            {
                lock ( _postingQueue )
                {
                    _postingQueue.Enqueue( new PostingData { InstallationId = installationId, Payload = payload, ProjectId = projectId, Apikey = apikey } );
                    Debug.WriteLine( _postingQueue.Count );
                }

                if ( !_isProcessing )
                {
                    _isProcessing = true;
                    ThreadPool.QueueUserWorkItem( ProcessHandler );
                }
            }
            catch ( Exception exception )
            {
                Logger.ErrorLog( "DataHelper", "PostObservationEnqueue", exception.ToString( ) );
            }
        }

        private static void ProcessHandler( object obj )
        {
            var startDate = DateTime.Now;
            var startStopWatch = new Stopwatch( );
            startStopWatch.Start( );

            try
            {
                var sb = new StringBuilder( );

                while ( _postingQueue.Count > 0 )
                {
                    var data = _postingQueue.Dequeue( );
                    try
                    {
                        sb.Clear( );
                        sb.Append( "https://api.switchautomation.com/v1/projects/" );
                        //sb.Append("http://localhost:28470/v1/projects/");
                        sb.Append( data.ProjectId );
                        sb.Append( "/installations/" );
                        sb.Append( data.InstallationId );
                        sb.Append( "/newreadings" );

                        var web = new WebClient( );
                        web.Headers.Add( "Accept:application/json" );
                        web.Headers.Add( "Content-Type:application/json" );
                        web.Headers.Add( "X-SwitchApiKey: " + data.Apikey );

                        var respArr = web.UploadData( sb.ToString( ), Encoding.ASCII.GetBytes( data.Payload ) );
                        var resp = Encoding.ASCII.GetString( respArr );

                        if ( resp.IndexOf( "successfully", StringComparison.Ordinal ) == -1 )
                        {
                            Logger.LocalLog( "DataHelper", "ProcessHandler", "LogMessage", resp );
                            Logger.LocalLog( "DataHelper", "ProcessHandler", "LogMessage", data.Payload );
                        }
                    }
                    catch ( Exception exception )
                    {
                        Logger.ErrorLog( "DataHelper", "ProcessHandler", "payload" + data.Payload );
                        Logger.ErrorLog( "DataHelper", "ProcessHandler", "Url: " + sb + "  ApiKey: " + data.Apikey + "  Error: " + exception );
                    }

                    //Debug.WriteLine( "proc: " + _postingQueue.Count );
                    Logger.LocalLog( "DataHelper", "ProcessHandler", "LogMessage", "proc: " + _postingQueue.Count );
                }

                //Console.WriteLine( "Done posting" );
                Logger.LocalLog( "DataHelper", "ProcessHandler", "LogMessage", "Done posting" );
            }
            catch ( Exception exception )
            {
                Logger.ErrorLog( "DataHelper", "ProcessHandler", exception.ToString() );
            }
            finally
            {
                _isProcessing = false;

                startStopWatch.Stop( );
                var ms = startStopWatch.ElapsedMilliseconds;

                Logger.LocalLog( "DataHelper", "ProcessHandler", "LogMessage", $"Weather Data Posting took about {ms}ms to finish... ({( startDate - DateTime.Now ).TotalMilliseconds}ms) {Environment.NewLine}" );
                
            }
        }

        public static void Upload( Stream file, string name, string containerName )
        {
            //bool result = false;
            try
            {
                var storageAccount = CloudStorageAccount.Parse( Invariables.SwitchStorageConnection );
                var blobClient = storageAccount.CreateCloudBlobClient( );
                var container = blobClient.GetContainerReference( containerName );
                var blob = container.GetBlockBlobReference( name );
                var blocklist = new HashSet<string>( );
                foreach ( FileBlock block in GetFileBlocks( file ) )
                {
                    blob.PutBlock( block.Id, new MemoryStream( block.Content, true ), null );
                    blocklist.Add( block.Id );
                }
                blob.PutBlockList( blocklist );
                //result = true;
            }
            catch ( Exception ex )
            {
                Console.WriteLine( ex.ToString( ) );
            }
            //return result;
        }

        #endregion

        #region MyRegion

        static double Radians( double x )
        {
            var PIx = 3.141592653589793;
            return x * PIx / 180;
        }

        static double KmsBetweenPoints( double lat1, double lon1, double lat2, double lon2 )
        {

            var RADIUS = 6378.16;

            var dlon = Radians( lon2 - lon1 );
            var dlat = Radians( lat2 - lat1 );

            var a = ( Math.Sin( dlat / 2 ) * Math.Sin( dlat / 2 ) ) + Math.Cos( Radians( lat1 ) ) * Math.Cos( Radians( lat2 ) ) * ( Math.Sin( dlon / 2 ) * Math.Sin( dlon / 2 ) );
            var angle = 2 * Math.Atan2( Math.Sqrt( a ), Math.Sqrt( 1 - a ) );
            return angle * RADIUS;
        }

        static DateTime GetEarliestRecord( string deviceId )
        {
            var endDate = DateTime.Now;
            try
            {
                var dt = SqlHelper.ExecuteReader( "select objectpropertyid from objectproperties where deviceid=@deviceid and propertyname='ExternalTemperature'",
                    new List<SqlParam> { new SqlParam( "deviceid", Guid.Parse( deviceId ), SqlDbType.UniqueIdentifier ) } );
                if ( dt != null && dt.Rows.Count > 0 )
                {
                    // got objectpropertyid for ExternalTemperature
                    var oid = dt.Rows[0][0].ToString( );

                    // retrieve earlist record
                    var dt1 = SqlHelper.ExecuteReader( Constants.ReadingSummaries, "select min([datetime]) from readingsummaries where objectpropertyid=@id",
                        new List<SqlParam> { new SqlParam( "id", Guid.Parse( oid ), SqlDbType.UniqueIdentifier ) } );
                    if ( dt1 != null && dt1.Rows.Count > 0 )
                        if ( !DateTime.TryParse( dt1.Rows[0][0].ToString( ), out endDate ) )
                            endDate = DateTime.Now;
                }
            }
            catch ( Exception ex )
            {
                Logger.ErrorLog( "Weatherutility", "GetEarliestRecord", ex.ToString( ) );
            }
            return endDate;
        }

        private static string RetrieveNearestAirport( string weatherStationDeviceId )
        {
            var airportCode = "";

            try
            {
                var web = new WebClient( );
                // reverse geocode the addresss
                Tuple<double, double> latlong = null;

                var sql = @"SELECT Latitude, Longitude FROM Installation WHERE WeatherStationDeviceID = @weatherId ";
                var tbl = SqlHelper.ExecuteReader( Invariables.SwitchConnection, sql, new List<SqlParam> { new SqlParam( "weatherId", weatherStationDeviceId, SqlDbType.NVarChar ) } );
                if ( tbl.Rows.Count > 0 )
                {
                    latlong = new Tuple<double, double>( double.Parse(tbl.Rows[0]["Latitude"].ToString() ), double.Parse( tbl.Rows[0]["Longitude"].ToString( ) ) );
                }

                if ( latlong != null )
                {
                    double latitude = latlong.Item1;
                    double longitude = latlong.Item2;

                    var geocodeResult = web.DownloadString( "http://maps.googleapis.com/maps/api/geocode/json?latlng=" + latitude + "," + longitude + "&sensor=false" );
                    //var geocodeResult = File.ReadAllText("c:/temp/virginia_mason_reverse.json.txt");

                    var json = JObject.Parse( geocodeResult );
                    if ( json != null )
                    {
                        string area = GetArea( geocodeResult );
                        //var alist = _airports.Where(a => a.Area == area);
                        Airport nearest = null;
                        double kms = 0;

                        if ( _airports == null )
                            _airports = LoadAirportData( out errormessage );

                        foreach ( var ap in _airports )
                        {
                            if (ap.Area != area) continue;

                            var calculatedKms = Math.Round( KmsBetweenPoints( ap.Latitude, ap.Longitude, latitude, longitude ), 2 );

                            if ( nearest == null )
                            {
                                nearest = ap;
                                kms = calculatedKms;
                            }
                            else if ( kms > calculatedKms )
                            {
                                nearest = ap;
                                kms = calculatedKms;
                            }
                        }

                        // 41.5422695	-81.4436759
                        if ( nearest != null )
                            airportCode = nearest.Code;

                    }
                }
            }
            catch ( Exception ex )
            {
                Logger.ErrorLog( "Weatherutility", "RetrieveNearestAirport", ex.ToString( ) );
            }
            return airportCode;
        }

        static string GetArea( string json )
        {
            var result = "";
            try
            {
                var country = new StringBuilder( );
                var locality = new StringBuilder( );
                var political = new StringBuilder( );
                var jobj = JObject.Parse( json );
                if (jobj?["results"]?[0] != null)
                {
                    country.Clear( );
                    locality.Clear( );

                    var level = 100;
                    foreach ( var o in jobj["results"][0]["address_components"] )
                    {

                        foreach ( var t in o["types"] )
                        {
                            if ( t.ToString( ) == "locality" && o["long_name"] != null )
                            {
                                locality.Clear( );
                                locality.Append( o["long_name"] );
                            }
                            else if ( t.ToString( ) == "country" && o["long_name"] != null )
                            {
                                country.Clear( );
                                country.Append( o["long_name"] );
                            }
                            else if ( t.ToString( ).IndexOf( "administrative", StringComparison.Ordinal ) > -1 && o["long_name"] != null )
                            {
                                var currentLevel = 0;
                                if ( int.TryParse( t.ToString( ).Replace( "administrative_area_level_", "" ), out currentLevel ) )
                                    if ( currentLevel < level )
                                    {
                                        level = currentLevel;

                                        political.Clear( );
                                        political.Append( o["long_name"] );
                                    }
                            }
                        }
                    }
                    if ( string.IsNullOrWhiteSpace( political.ToString( ) ) )
                        political.Clear( ).Append( locality );
                    result = political + "," + country;
                }
            }
            catch ( Exception )
            {
            }
            return result;
        }

        #endregion

    }

}