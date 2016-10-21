using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
//using Switch.Connections;
using Switch.Sql;
using Switch.Table;
using Switch.Time;
using WeatherUtility.Helpers;
using WeatherUtility.Models;
using WeatherUtility.Utilities;

namespace WeatherUtility
{
    public partial class WeatherUtilityForm : Form
    {
        #region ' Constructor / Destructor '

        public WeatherUtilityForm()
        {
            InitializeComponent();
            Shown += WeatherUtilityForm_Shown;
        }

        #endregion

        #region ' Global Variables '

        private string _apikey;
        private string _projectId;

        #endregion

        #region ' Methods / Functions '

        private void InitializeObjects()
        {
            try
            {
                var projectkey = DataHelper.GetApiProjectKeys(Invariables.wwoInstallation); //item1 key / item2 id
                _apikey = projectkey.Item1;
                _projectId = projectkey.Item2;

                LoadAllExistingWeatherDevices();
                tabShowSelection.SelectedTab = tabPageByStation;
                tabShowSelection.SelectedIndexChanged += TabShowSelection_SelectedIndexChanged;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        private void TabShowSelection_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabShowSelection.SelectedTab == tabPageBySite)
                LoadExistingInstallationsWithWeatherDevice();
            else
                LoadAllExistingWeatherDevices();
        }

        private void LoadExistingInstallationsWithWeatherDevice()
        {
            try
            {
                progressStatusText.Text = "Loading existing weather stations by installation...";
                progressDownloadBar.Value = 25;

                Thread.Sleep(1000);
                progressDownloadBar.Value = 55;

                // TODO :: This sql query will change eventually when the invalid installations are deleted.
                var sql = @"SELECT 	CAST(0 AS bit) AS 'Select', InstallationID, InstallationName, Latitude, Longitude, 
			                                WeatherStationDeviceID, WeatherForecastDeviceID, Address, StateName, Country, Suburb 
                            FROM 	Installation A INNER JOIN ApiProjects B ON A.ApiProjectID = b.ProjectID 
                            WHERE 	(WeatherStationDeviceID IS NOT NULL OR WeatherForecastDeviceID IS NOT NULL) 
			                                AND (WeatherStationDeviceID != '00000000-0000-0000-0000-000000000000')
			                                AND Latitude<>0 AND Longitude<>0 AND Latitude<>-1 AND StateName<>'' 
			                                AND InstallationName IS NOT NULL AND (Address IS NOT NULL OR Address <>'')
			                                AND InstallationName NOT IN ('','remove me','Delete building','Stefan Test','house','Home','NA','N/A') 
                                            AND InstallationName NOT LIKE '%delete%' AND A.projectID NOT IN ('36d26edb')
			                                AND (b.IsRemoved = 0 OR b.IsRemoved IS NULL) ";
                //AND (WeatherStationDeviceID = '4a4fc263-dbd7-4e78-ac52-6d5efa5e0ddf') "; //remove filter after testing

                Thread.Sleep(3000);
                progressDownloadBar.Value = 85;

                var installDT = SqlHelper.ExecuteReader(Invariables.SwitchConnection, sql, new List<SqlParam>());
                if (installDT != null && installDT.Rows.Count > 0)
                {
                    gridByInstallations.DataSource = installDT;
                    progressStatusText.Text = $"Loaded {installDT.Rows.Count} installations with weather stations";
                }
                else
                    progressStatusText.Text = "No existing weather stations found";

                progressDownloadBar.Value = 100;
                Logger.LocalLog("WeatherUtility", "ProcessNewInstallation", "LogMessage", progressStatusText.Text);
            }
            catch (Exception ex)
            {
                progressStatusText.Text = ex.Message;
                progressDownloadBar.Value = 10;
                Logger.ErrorLog("WeatherUtility", "ProcessNewInstallation", ex.ToString());
            }
        }

        private void LoadAllExistingWeatherDevices()
        {
            try
            {
                progressStatusText.Text = "Loading all existing weather stations...";
                progressDownloadBar.Value = 25;

                Thread.Sleep(1000);
                progressDownloadBar.Value = 55;

                // TODO :: This sql query will change eventually when the invalid installations are deleted.
                var sql = @"SELECT CAST(0 AS bit) AS 'Select', C.DeviceName,C.DeviceID AS WeatherStationDeviceID 
                            FROM DeviceSpecs B INNER JOIN 
			                            Devices C ON B.DeviceID=C.DeviceID 
                            WHERE B.DriverDeviceType='WeatherStation' 
                            ORDER BY C.DeviceName ";

                Thread.Sleep(3000);
                progressDownloadBar.Value = 85;

                var weatherDevices = SqlHelper.ExecuteReader(Invariables.SwitchConnection, sql, new List<SqlParam>());
                if (weatherDevices != null && weatherDevices.Rows.Count > 0)
                {
                    gridByWeatherStations.DataSource = weatherDevices;
                    progressStatusText.Text = $"Loaded {weatherDevices.Rows.Count} weather stations";
                }
                else
                    progressStatusText.Text = "No existing weather stations found";

                progressDownloadBar.Value = 100;
                Logger.LocalLog("WeatherUtility", "LoadAllExistingWeatherDevices", "LogMessage", progressStatusText.Text);
            }
            catch (Exception ex)
            {
                progressStatusText.Text = ex.Message;
                progressDownloadBar.Value = 10;
                Logger.ErrorLog("WeatherUtility", "LoadAllExistingWeatherDevices", ex.ToString());
            }
        }

        private void DownloadAirportHistorical(List<AirportDownloadModel> airportDownload)
        {
            try
            {
                Logger.LocalLog("WeatherUtility", "DownloadAirportHistorical", "LogMessage",
                    "Processing download list...");

                var web = new WebClient();
                foreach (var apdl in airportDownload)
                {
                    if ((apdl.EndDownload - apdl.StartDownload).TotalDays > 365)
                    {
                        Logger.LocalLog("WeatherUtility", "DownloadAirportHistorical", "LogMessage",
                            "Download 1 year only " + apdl.StartDownload.ToString("yyyy-MM-dd") + " to " +
                            apdl.EndDownload.ToString("yyyy-MM-dd"));
                        apdl.StartDownload = apdl.EndDownload.AddDays(-365);
                    }

                    var current = apdl.StartDownload;
                    var weatherStation = DataHelper.GetStation(apdl.WeatherStationDeviceId);
                    if (weatherStation == null)
                        return;

                    var payload = new StringBuilder();

                    var currentTime = DateTime.Now.Date;
                    Logger.LocalLog("WeatherUtility", "DownloadAirportHistorical", "LogMessage",
                        $"Downloading airport : {apdl.AirportCode} from {apdl.StartDownload} to {apdl.EndDownload}");

                    while (current <= apdl.EndDownload)
                    {
                        try
                        {
                            // compute hdd and cdd before adding up a day
                            int offset = -1;
                            TimeHelper.GetUtcOffsetForLocalDatetimeForInstallation(
                                Guid.Parse( Invariables.wwoInstallation ), current.Date, out offset );

                            offset = ( -1 * offset );
                            var utcStart = DateTime.SpecifyKind( current.Date.AddMinutes( offset ), DateTimeKind.Utc );
                            var utcEnd = DateTime.SpecifyKind(
                                current.Date.AddDays( 1 ).AddSeconds( -1 ).AddMinutes( offset ), DateTimeKind.Utc );

                            PostDegreeDayValuesForPreviousDay( weatherStation.DeviceId, utcStart, utcEnd );

                            //string[] lines = null;
                            //Logger.LocalLog("WeatherUtility", "DownloadAirportHistorical", "LogMessage",
                            //    DateTime.Now + " Downloading airport : " + apdl.AirportCode + " Date " + current);

                            //var result = web.DownloadString("http://www.wunderground.com/history/airport/" +
                            //                                apdl.AirportCode + "/" + current.Year + "/" + current.Month +
                            //                                "/" + current.Day +
                            //                                "/DailyHistory.html?req_city=NA&req_state=NA&req_statename=NA&format=1");

                            //File.AppendAllText(@"C:\Logs\WUndergroundHistoricalData.csv", result);

                            //var fn = apdl.AirportCode + "/" + apdl.AirportCode + "_" + current.ToString("yyyyMMdd") + ".csv";
                            //result = result.Replace("\n", "");
                            //result = result.Replace("<br />", "\r");
                            //lines = result.Split('\r');

                            //// retreive the weatherstations
                            //var obj = new AirportDownloadModel
                            //{
                            //    AirportCode = apdl.AirportCode,
                            //    Downloaded = false,
                            //    EndDownload = apdl.EndDownload,
                            //    StartDownload = current,
                            //    WeatherStationDeviceId = apdl.WeatherStationDeviceId
                            //};

                            //var doUpload = true;
                            //var convertToCelcius = false;

                            //// Check missing interval readings only
                            //// Get data of the missing interval readings
                            //var offset = 0;
                            //TimeHelper.GetUtcOffsetForLocalDatetimeForInstallation(
                            //    Guid.Parse(Invariables.wwoInstallation), current.Date, out offset);

                            //offset = (-1*offset);
                            //var utcStart = DateTime.SpecifyKind(current.Date.AddMinutes(offset), DateTimeKind.Utc);
                            //var utcEnd = DateTime.SpecifyKind(
                            //    current.Date.AddDays(1).AddSeconds(-1).AddMinutes(offset), DateTimeKind.Utc);

                            //var errormessage = "";
                            //var sensorList = DataHelper.GetFirstSensor(Guid.Parse(apdl.WeatherStationDeviceId),
                            //    out errormessage);
                            //var getReadings = new List<WeatherReading>();

                            //foreach (var sensor in sensorList)
                            //{
                            //    var rowfilter =
                            //        $"PartitionKey eq '{Invariables.wwoInstallation}' and RowKey ge '{sensor}:{(utcStart.AddMinutes(-15)).Ticks}' and RowKey le '{sensor}:{utcEnd.Ticks}'";
                            //    getReadings.AddRange(
                            //        TableStorageRestHelper.QueryEntities<WeatherReading>(
                            //            Invariables.SwitchStorageConnection, Invariables.readingsTableName, rowfilter, 500000));
                            //}

                            //// delete interval reading record
                            //var c = 0;
                            //var isDeleted = false;
                            //while (!isDeleted & c < 5)
                            //{
                            //    c++;
                            //    isDeleted = TableStorageRestHelper.DeleteEntities(Invariables.SwitchStorageConnection,
                            //        Invariables.readingsTableName, Invariables.wwoInstallation,
                            //        getReadings.Cast<object>().ToList());
                            //}

                            //for (var i = 0; i < lines.Length; i++)
                            //{
                            //    try
                            //    {
                            //        if (!string.IsNullOrWhiteSpace(lines[i]))
                            //        {
                            //            if (i == 0)
                            //            {
                            //                if (!lines[i].ToLower().Contains("airport code,"))
                            //                    lines[i] = "Airport Code," + lines[i];
                            //                var tokens2 = DataHelper.StringSplit(lines[i]);
                            //                if (tokens2[2].ToLower() == "temperaturef")
                            //                    convertToCelcius = true;
                            //            }
                            //            else
                            //            {
                            //                var isMissing = false;
                            //                var tokens = DataHelper.StringSplit(lines[i]);
                            //                if (tokens[0] != apdl.AirportCode)
                            //                {
                            //                    lines[i] = apdl.AirportCode + "," + lines[i];
                            //                    tokens = DataHelper.StringSplit(lines[i]);
                            //                }

                            //                DateTime.TryParse(tokens[14], out currentTime);
                            //                currentTime = DateTime.SpecifyKind(currentTime, DateTimeKind.Utc);
                            //                var currentTimeString =
                            //                    (currentTime.ToUniversalTime()).ToUniversalTime()
                            //                        .ToString("yyyy-MM-ddTHH:00:00Z");
                            //                isMissing = true;

                            //                double externalTemperature;
                            //                double.TryParse(tokens[2], out externalTemperature);
                            //                if (convertToCelcius)
                            //                    externalTemperature = Math.Round((externalTemperature - 32)*0.5555, 2);

                            //                double humidity;
                            //                double.TryParse(tokens[4], out humidity);

                            //                double pressure;
                            //                double.TryParse(tokens[5], out pressure);

                            //                double visibility;
                            //                double.TryParse(tokens[6], out visibility);

                            //                double winddirection;
                            //                double.TryParse(tokens[7], out winddirection);

                            //                double windspeed;
                            //                double.TryParse(tokens[8], out windspeed);

                            //                double currentRain;
                            //                double.TryParse(tokens[10], out currentRain);

                            //                if (isMissing)
                            //                {
                            //                    payload.Clear().Append("[");
                            //                    // we need to filter external temperature that is <200 
                            //                    // coming from wunderground

                            //                    // external temperature
                            //                    if (externalTemperature > -200 && externalTemperature < 400)
                            //                    {
                            //                        if (
                            //                            weatherStation.StationProperties.ContainsKey(
                            //                                "ExternalTemperature"))
                            //                            payload.Append(DataHelper.GeneratePayloadEntry(payload,
                            //                                weatherStation.StationProperties["ExternalTemperature"],
                            //                                currentTimeString, externalTemperature.ToString("N2")));
                            //                    }

                            //                    // humidity
                            //                    if (weatherStation.StationProperties.ContainsKey("Humidity"))
                            //                        payload.Append(DataHelper.GeneratePayloadEntry(payload,
                            //                            weatherStation.StationProperties["Humidity"],
                            //                            currentTimeString, humidity.ToString("N2")));

                            //                    // windspeed
                            //                    if (weatherStation.StationProperties.ContainsKey("WindSpeed"))
                            //                        payload.Append(DataHelper.GeneratePayloadEntry(payload,
                            //                            weatherStation.StationProperties["WindSpeed"],
                            //                            currentTimeString, windspeed.ToString("N2")));


                            //                    // wind direction
                            //                    if (weatherStation.StationProperties.ContainsKey("WindDirection"))
                            //                        payload.Append(DataHelper.GeneratePayloadEntry(payload,
                            //                            weatherStation.StationProperties["WindDirection"],
                            //                            currentTimeString, winddirection.ToString("N2")));

                            //                    // barometric pressure
                            //                    if (weatherStation.StationProperties.ContainsKey("BarometricPressure"))
                            //                        payload.Append(DataHelper.GeneratePayloadEntry(payload,
                            //                            weatherStation.StationProperties["BarometricPressure"],
                            //                            currentTimeString, pressure.ToString("N2")));

                            //                    // visibility
                            //                    if (weatherStation.StationProperties.ContainsKey("Visibility"))
                            //                        payload.Append(DataHelper.GeneratePayloadEntry(payload,
                            //                            weatherStation.StationProperties["Visibility"],
                            //                            currentTimeString, visibility.ToString("N2")));

                            //                    // compute the wet bulb
                            //                    if (weatherStation.StationProperties.ContainsKey("WetBulbTemperature"))
                            //                    {
                            //                        var tempc = externalTemperature;
                            //                        var rh = humidity;

                            //                        var wetbulbTemperatureF =
                            //                            DataHelper.ComputeWetBulbTemperature(
                            //                                DataHelper.CelciusToF(tempc), rh);
                            //                        if (!double.IsNaN(wetbulbTemperatureF))
                            //                        {
                            //                            payload.Append(DataHelper.GeneratePayloadEntry(payload,
                            //                                weatherStation.StationProperties["WetBulbTemperature"],
                            //                                currentTimeString,
                            //                                DataHelper.FahrenheitToC(wetbulbTemperatureF).ToString("N2")));
                            //                        }
                            //                    }

                            //                    // rain & current rain
                            //                    double sample = 0;
                            //                    currentRain = Math.Round(currentRain, 2);

                            //                    if (weatherStation.StationProperties.ContainsKey("RainCurrent"))
                            //                    {
                            //                        var prevrain = weatherStation.CurrentRain;
                            //                        if (prevrain > currentRain)
                            //                            sample = currentRain;
                            //                        else
                            //                            sample = currentRain - prevrain;

                            //                        if (weatherStation.StationProperties.ContainsKey("RainCurrent"))
                            //                            payload.Append(DataHelper.GeneratePayloadEntry(payload,
                            //                                weatherStation.StationProperties["RainCurrent"],
                            //                                currentTimeString, currentRain.ToString("N2")));

                            //                        if (weatherStation.StationProperties.ContainsKey("Rain"))
                            //                            payload.Append(DataHelper.GeneratePayloadEntry(payload,
                            //                                weatherStation.StationProperties["Rain"],
                            //                                currentTimeString, sample.ToString("N2")));

                            //                        weatherStation.CurrentRain = currentRain;
                            //                    }
                            //                    else if (weatherStation.StationProperties.ContainsKey("Rain"))
                            //                        payload.Append(DataHelper.GeneratePayloadEntry(payload,
                            //                            weatherStation.StationProperties["Rain"],
                            //                            currentTimeString, sample.ToString("N2")));

                            //                    payload.Append("]");

                            //                    DataHelper.PostObservationEnqueue(_projectId,
                            //                        Invariables.wwoInstallation, payload.ToString(), _apikey);
                            //                }
                            //            }
                            //        }
                            //    }
                            //    catch (Exception)
                            //    {
                            //    }

                            //    if (i%10 == 0)
                            //        Thread.Sleep(10);
                            //}
                            //if (doUpload)
                            //{
                            //    //Logger.LocalLog( "WeatherUtility", "DownloadAirportHistorical", "LogMessage", "Uploading to container [" + Invariables.weatherHistory + "] file: " + fn );
                            //    File.WriteAllLines(@"C:\temp\temp.tmp", lines);
                            //    DataHelper.Upload(new MemoryStream(File.ReadAllBytes(@"C:\temp\temp.tmp")), fn,
                            //        Invariables.weatherHistory);
                            //}

                            //// update the entry
                            //obj.AirportCode = apdl.AirportCode;
                            //obj.Downloaded = false;
                            //obj.EndDownload = apdl.EndDownload;
                            //obj.StartDownload = current;
                            //obj.WeatherStationDeviceId = weatherStation.DeviceId.ToString();

                            //var retry = 0;
                            //var ok = false;
                            //while (!ok && retry < 5)
                            //{
                            //    retry++;
                            //    Thread.Sleep(5);
                            //    ok = TableStorageRestHelper.InsertOrReplaceEntity(Invariables.SwitchStorageConnection,
                            //        Invariables.weatherHistoryTableName, Invariables.partitionKey, apdl.AirportCode, obj);
                            //}

                        }
                        catch (Exception ex)
                        {
                            Logger.ErrorLog("WeatherUtility", "ProcessNewInstallation", ex.ToString());
                        }
                        finally
                        {
                            current = current.AddDays(1);
                            Thread.Sleep(10);
                        }
                    }

                    if (current >= apdl.EndDownload)
                    {
                        TableStorageRestHelper.DeleteEntity(Invariables.SwitchStorageConnection,
                            Invariables.weatherHistoryTableName, Invariables.partitionKey, apdl.AirportCode);
                        Logger.LocalLog("WeatherUtility", "DownloadAirportHistorical", "LogMessage",
                            $"Done downloading {apdl.AirportCode}.");
                    }
                }
            }
            catch (Exception ex)
            {
                progressStatusText.Text = ex.Message;
                progressDownloadBar.Value = 10;
                Logger.LocalLog("WeatherUtility", "DownloadAirportHistorical", "LogMessage", ex.ToString());
            }
        }

        private void PostDegreeDayValuesForPreviousDay( Guid deviceId, DateTime startDateTime, DateTime endDateTime )
        {
            try
            {
                string cddSensor = "";
                string hddSensor = "";
                string extTempSensor = "";

                StringBuilder payload = new StringBuilder( );

                // get cdd / hdd sensors of weatherstation
                var sql = @"SELECT ObjectPropertyId, PropertyName 
                            FROM ObjectProperties 
                            WHERE DeviceId = @device 
	                            AND (PropertyName LIKE '%externaltemperature%' OR 
										PropertyName LIKE '%degreeday%')";

                var dt = SqlHelper.ExecuteReader( Invariables.SwitchConnection, sql,
                        new List<SqlParam> { new SqlParam( "device", deviceId, SqlDbType.UniqueIdentifier ) } );

                if ( dt != null && dt.Rows.Count > 0 )
                {
                    foreach ( DataRow nRow in dt.Rows )
                    {
                        switch ( nRow["PropertyName"].ToString( ).ToLower( ) )
                        {
                            case "externaltemperature":
                                extTempSensor = nRow["ObjectPropertyId"].ToString( );
                                break;
                            case "coolingdegreeday":
                                cddSensor = nRow["ObjectPropertyId"].ToString( );
                                break;
                            case "heatingdegreeday":
                                hddSensor = nRow["ObjectPropertyId"].ToString( );
                                break;
                        }
                    }
                }

                string rowFilter = $"PartitionKey eq '{Invariables.wwoInstallation}' and RowKey ge '{extTempSensor}:{startDateTime.Ticks}' and RowKey le '{extTempSensor}:{endDateTime.Ticks}'";
                var readings = TableStorageRestHelper.QueryEntities<WeatherReading>( Invariables.SwitchStorageConnection, "ObservationStaging", rowFilter );

                decimal totalHdd = 0;
                decimal totalCdd = 0;
                decimal partOfDay = decimal.Parse( "1" ) / decimal.Parse( readings.Count.ToString( ) );

                foreach ( WeatherReading reading in readings )
                {
                    decimal heating, cooling;
                    decimal baseTemperature = decimal.Parse( "18" );

                    heating = ( baseTemperature - decimal.Parse( reading.Value.ToString(CultureInfo.InvariantCulture) ) ) * partOfDay;
                    cooling = ( decimal.Parse( reading.Value.ToString(CultureInfo.InvariantCulture) ) - baseTemperature ) * partOfDay;

                    decimal hdd = ( heating ) < 0 ? 0 : heating;
                    decimal cdd = ( cooling ) < 0 ? 0 : cooling;

                    totalHdd = totalHdd + hdd;
                    totalCdd = totalCdd + cdd;
                }

                payload.Append( "[" );

                // heating degree day
                payload.Append( DataHelper.GeneratePayloadEntry( payload,
                        hddSensor, endDateTime.ToString( "yyyy-MM-ddT16:00:00Z" ),
                        Math.Round( totalHdd, 4 ).ToString(CultureInfo.InvariantCulture) ) );

                // cooling degree day
                payload.Append( DataHelper.GeneratePayloadEntry( payload,
                        cddSensor, endDateTime.ToString( "yyyy-MM-ddT16:00:00Z" ),
                        Math.Round( totalCdd, 4 ).ToString(CultureInfo.InvariantCulture) ) );

                payload.Append( "]" );

                // Post CDD & HDD
                DataHelper.PostObservationEnqueue( _projectId, Invariables.wwoInstallation,
                            payload.ToString( ), _apikey );
            }
            catch ( Exception ex )
            {
                progressStatusText.Text = ex.Message;
                progressDownloadBar.Value = 10;
                Logger.LocalLog( "WeatherUtility", "PostDegreeDayValuesForPreviousDay", "LogMessage", ex.ToString( ) );
            }
        }

        #endregion

        #region ' Events '

        private void WeatherUtilityForm_Shown(object sender, EventArgs e)
        {
            InitializeObjects();
        }

        private void buttonDownload_Click(object sender, EventArgs e)
        {
            try
            {
                progressStatusText.Text = "";
                progressDownloadBar.Value = 0;

                if (dateTimeStart.Value == dateTimeEnd.Value || dateTimeEnd.Value < dateTimeStart.Value)
                {
                    MessageBox.Show("Invalid date range.");
                    return;
                }

                var _selectedWeatherDevices = new List<AirportDownloadModel>();
                var airportcode = "";
                var start = dateTimeStart.Value.Date;
                var end = dateTimeEnd.Value.Date.AddDays(1).AddSeconds(-1);

                if (tabShowSelection.SelectedTab == tabPageByStation)
                {
                    var tbl = (DataTable) gridByWeatherStations.DataSource;
                    var selectedItems = tbl.Select("Select = 1");

                    foreach (DataRow i in tbl.Rows)  //selectedItems)
                    {
                        airportcode = DataHelper.GetHistory(i["WeatherStationDeviceID"].ToString());
                        _selectedWeatherDevices.Add(new AirportDownloadModel
                        {
                            AirportCode = airportcode,
                            StartDownload = start,
                            EndDownload = end,
                            LastDownload = DateTime.Now,
                            Downloaded = false,
                            WeatherStationDeviceId = i["WeatherStationDeviceID"].ToString()
                        });
                    }

                    MessageBox.Show(
                        $"Selected {_selectedWeatherDevices.Count} weather devices to download historical data.");

                    if (_selectedWeatherDevices.Count > 0)
                        DownloadAirportHistorical(_selectedWeatherDevices);
                    else
                        progressStatusText.Text = "Nothing to download.";
                }
                else if (tabShowSelection.SelectedTab == tabPageBySite)
                {
                    var tbl = (DataTable) gridByInstallations.DataSource;
                    var selectedItems = tbl.Select("Select = 1");

                    foreach ( DataRow i in tbl.Rows) //selectedItems )
                    {
                        airportcode = DataHelper.GetHistory(i["WeatherStationDeviceID"].ToString());
                        _selectedWeatherDevices.Add(new AirportDownloadModel
                        {
                            AirportCode = airportcode,
                            StartDownload = start,
                            EndDownload = end,
                            LastDownload = DateTime.Now,
                            Downloaded = false,
                            WeatherStationDeviceId = i["WeatherStationDeviceID"].ToString()
                        });
                    }

                    if (_selectedWeatherDevices.Count > 0)
                        DownloadAirportHistorical(_selectedWeatherDevices);
                    else
                        progressStatusText.Text = "Nothing to download.";
                }
            }
            catch (Exception ex)
            {
                progressStatusText.Text = ex.Message;
                progressDownloadBar.Value = 10;
                Logger.ErrorLog("WeatherUtility", "buttonDownload_Click", ex.ToString());
            }
        }

        #endregion
    }
}