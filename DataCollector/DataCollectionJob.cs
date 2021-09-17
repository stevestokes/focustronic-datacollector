using DeltaComparer;
using Newtonsoft.Json;
using Quartz;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DataCollector
{
    public class DataCollectionJob : IJob
    {
        private bool log = false;
        private string logfile = "";

        public async Task Execute(IJobExecutionContext context)
        {
            var configFile = "config.json";

            if (!File.Exists(configFile))
            {
                Log($"ERROR: Missing configuration file: {configFile}", skipLog: true);
            }

            var configContents = File.ReadAllText(configFile);

            var config = JsonConvert.DeserializeObject<dynamic>(configContents);

            //var mastertronicLocalIP = config.mastertronicLocalIP.Value;
            var mastertronicCommand = config.mastertronicCommand.Value;
            var alkatronicEndpoint = config.alkatronicEndpoint.Value;
            var alkatronicMeasurementsCommand = config.alkatronicMeasurementsCommand.Value;
            var alkatronicDevicesCommand = config.alkatronicDevicesCommand.Value;
            var alkatronicLoginCommand = config.alkatronicLoginCommand.Value;
            var alkatronicEmail = config.alkatronicEmail.Value;
            var alkatronicPassword = config.alkatronicPassword.Value;
            var measurementsBaseEndpoint = config.measurementsBaseEndpoint.Value;
            var measurementsEndpoint = config.measurementsEndpoint.Value;
            var measurementsAlkatronicEndpoint = config.measurementsAlkatronicEndpoint.Value;
            var measurementsAPIKey = config.measurementsAPIKey.Value;
            var measurementsCommand = config.measurementsCommand.Value;
            var logging = config.logging.Value;
            var logFile = config.logFile.Value;
            var parameters = config.parameters.ToObject<List<string>>() as List<string>; // todo: validate parameter values

            if (string.IsNullOrEmpty(alkatronicEndpoint))
                Log($"ERROR: could not find alkatronicEndpoint in config file", skipLog: true);
            if (string.IsNullOrEmpty(alkatronicMeasurementsCommand))
                Log($"ERROR: could not find alkatronicMeasurementsCommand in config file", skipLog: true);
            if (string.IsNullOrEmpty(alkatronicDevicesCommand))
                Log($"ERROR: could not find alkatronicDevicesCommand in config file", skipLog: true);
            if (string.IsNullOrEmpty(mastertronicCommand))
                Log($"ERROR: could not find mastertronicCommand in config file", skipLog: true);
            if (string.IsNullOrEmpty(logging))
                Log($"ERROR: could not find logging in config file", skipLog: true);
            if (string.IsNullOrEmpty(logFile))
                Log($"ERROR: could not find logFile in config file", skipLog: true);
            if (!parameters.Any())
                Log($"ERROR: could not find parameters in config file", skipLog: true);
            if (string.IsNullOrEmpty(measurementsBaseEndpoint))
                Log($"ERROR: could not find measurementsBaseEndpoint in config file", skipLog: true);
            if (string.IsNullOrEmpty(measurementsEndpoint))
                Log($"ERROR: could not find measurementsEndpoint in config file", skipLog: true);
            if (string.IsNullOrEmpty(measurementsAlkatronicEndpoint))
                Log($"ERROR: could not find measurementsAlkatronicEndpoint in config file", skipLog: true);
            if (string.IsNullOrEmpty(measurementsAPIKey))
                Log($"ERROR: could not find measurementsAPIKey in config file", skipLog: true);
            if (string.IsNullOrEmpty(measurementsCommand))
                Log($"ERROR: could not find measurementsCommand in config file", skipLog: true);

            if (logging == "true")
            {
                log = true;
                logfile = logFile;
            }

            
            Log("Logging in to Focustronic/Alkatronic", true);

            var loginResponse = await LoginToAlkatronic(alkatronicEndpoint + alkatronicLoginCommand, alkatronicEmail, alkatronicPassword) as AlkatronicLoginReponse;

            Log("=> OK");

            var devices = await GetDevicesFromFocustronic(alkatronicEndpoint, alkatronicDevicesCommand, loginResponse.data) as IEnumerable<AlkatronicDevices>;

            Log("Searching for Alkatronic");

            // validate there is an alkatronic
            var hasAlkatronic = devices.Where(ad => ad.type == "alkatronic")
                                       .Any();

            if (hasAlkatronic)
            {
                Log("Starting data collection for Alkatronic");

                var originalAlkatronicMeasurements = await GetAlkatronicDataFromMeasurementsEndpoint(measurementsBaseEndpoint + measurementsAlkatronicEndpoint, measurementsAPIKey, context.CancellationToken) as IEnumerable<AlkatronicMeasurement>;

                var modifiedAlkatronicMeasurements = GetAlkatronicDataFromFocustronic(alkatronicEndpoint, alkatronicMeasurementsCommand, alkatronicDevicesCommand, loginResponse.data) as IEnumerable<AlkatronicMeasurement>;

                // delta results / hash
                var alkaDeltaResults = Delta.Compare(
                    modifieds: modifiedAlkatronicMeasurements,
                    originals: originalAlkatronicMeasurements,
                    m => m.record_time);

                if (alkaDeltaResults.Adds.Any())
                {
                    AddAlkatronicDataToMeasurementsEndpoint(measurementsBaseEndpoint + measurementsAlkatronicEndpoint, alkaDeltaResults.Adds, measurementsAPIKey);
                }

                Log("Done");
            }


            Log("Searching for Mastertronic");

            // get the local ip of the mastertronic
            var mastertronicLocalIP = devices.Where(ad => ad.type == "mastertronic")
                                             .Select(ad => ad.devices.FirstOrDefault().local_ip_address)
                                             .FirstOrDefault() ?? string.Empty;

            if (!string.IsNullOrEmpty(mastertronicLocalIP))
            {
                Log("Starting data collection for Mastertronic");

                var originalMastertronicMeasurements = GetMastertronicDataFromMeasurementsEndpoint(measurementsBaseEndpoint + measurementsEndpoint, measurementsAPIKey) as IEnumerable<Measurement>;

                var modifiedMastertronicMeasurements = GetMastertronicDataFromMastertronic(mastertronicLocalIP, mastertronicCommand, parameters) as IEnumerable<Measurement>;

                // delta results / hash
                var masterDeltaResults = Delta.Compare(
                    modifieds: modifiedMastertronicMeasurements,
                    originals: originalMastertronicMeasurements,
                    m => m.record_time, m => m.parameter);

                if (masterDeltaResults.Adds.Any())
                {
                    AddMastertronicDataToMeasurementsEndpoint(measurementsBaseEndpoint + measurementsEndpoint, masterDeltaResults.Adds, measurementsAPIKey);
                }

                // schedule every hour

                Log("Done");
            }
        }

        private void Log(string message, bool Write = false, bool skipLog = false, bool header = true)
        {
            var hdr = $"[{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}] - ";

            if (header)
                message = hdr + message;

            if (Write)
                Console.Write(message);
            else
                Console.WriteLine(message);

            if (log && !skipLog)
                using (var writer = File.AppendText(logfile))
                {
                    if (Write)
                        writer.Write(message);
                    else
                        writer.WriteLine(message);
                }
        }

        #region Mastertronic

        private IEnumerable<Measurement> GetMastertronicDataFromMastertronic(string ip, string command, IEnumerable<string> parameters)
        {
            //http request data in mastertronic

            var modifiedMeasurements = new List<Measurement>();

            var localURL = $"http://{ip}/";
            var client = new RestClient(localURL);

            foreach (var parameter in parameters)
            {
                try
                {
                    var mastertronicRequest = new MastertronicRequest()
                    {
                        action = "HISTORY",
                        data = new MastertronicRequestData()
                        {
                            @parameter = parameter,
                            range = "2160"
                        },
                        timestamp = DateTime.UtcNow.Ticks,
                        type = "read",
                    };

                    Log($"Requesting {parameter} from Mastertronic", Write: true);

                    var request = new RestRequest($"{command}{DateTime.UtcNow.Ticks}", DataFormat.Json)
                        .AddJsonBody(mastertronicRequest);

                    var response = client.Post(request);

                    var mastertronicResponse = JsonConvert.DeserializeObject<MastertronicResponse>(response.Content);

                    Log($" => OK", header: false);

                    modifiedMeasurements.AddRange(mastertronicResponse.data);
                }
                catch (Exception ex)
                {
                    Log($"ERROR: could not fetch data from MT\r\nparam: {parameter}, url: {client.BaseUrl}\r\n\r\n");
                    Log(ex.ToString());
                    throw;
                }
            }

            return modifiedMeasurements;
        }

        private IEnumerable<Measurement> GetMastertronicDataFromMeasurementsEndpoint(string measurementsEndpoint, string measurementsAPIKey)
        {
            //http request data in mastertronic

            var originalMeasurements = new List<Measurement>();
            var client = new RestClient(measurementsEndpoint);

            try
            {
                Log($"Requesting requesting data from Measurements Endpoint", Write: true);

                var request = new RestRequest()
                    .AddHeader("x-api-key", measurementsAPIKey);
                var response = client.Get(request);
                var measurementsResponse = JsonConvert.DeserializeObject<IEnumerable<Measurement>>(response.Content);

                Log($" => OK", header: false);

                originalMeasurements.AddRange(measurementsResponse);
            }
            catch (Exception ex)
            {
                Log($"ERROR: could not fetch data from Measurements Endpoint\r\nurl: {client.BaseUrl}\r\n\r\n");
                Log(ex.ToString());
                throw;
            }

            return originalMeasurements;
        }

        private void AddMastertronicDataToMeasurementsEndpoint(string endpoint, IEnumerable<Measurement> measurements, string measurementsAPIKey)
        {
            // post data to lambda

            var client = new RestClient(endpoint);

            try
            {
                Log($"Posting data to Measurements Endpoint", Write: true);

                var measurementsRequest = new MeasurementsRequest()
                {
                    Measurements = measurements,
                };

                var request = new RestRequest()
                    .AddHeader("x-api-key", measurementsAPIKey)
                    .AddJsonBody(measurements);

                var response = client.Post(request);

                var measurementsResponse = JsonConvert.DeserializeObject<IEnumerable<Measurement>>(response.Content);

                Log($" => OK", header: false);
            }
            catch (Exception ex)
            {
                Log($"ERROR: could not fetch data from Measurements Endpoint\r\nurl: {client.BaseUrl}\r\n\r\n");
                Log(ex.ToString());
                throw;
            }
        }

        #endregion

        #region Alkatronic

        private async Task<AlkatronicLoginReponse> LoginToAlkatronic(string url, string username, string password)
        {
            var result = string.Empty;
            var parameters = "email=" + username + "&password=" + password + "&platform=web";
            var request = (HttpWebRequest)WebRequest.Create(url);

            request.Method = "POST";
            request.ContentLength = parameters.Length;
            request.ContentType = "application/x-www-form-urlencoded";

            try
            {
                using (var sw = new StreamWriter(request.GetRequestStream()))
                {
                    sw.Write(parameters);
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }

            var response = await request.GetResponseAsync() as HttpWebResponse;

            using (var sr = new StreamReader(response.GetResponseStream()))
            {
                result = sr.ReadToEnd();
            }

            return JsonConvert.DeserializeObject<AlkatronicLoginReponse>(result);
        }

        private async Task<IEnumerable<AlkatronicDevices>> GetDevicesFromFocustronic(string endpoint, string command, string token)
        {
            try
            {
                // get device list

                var client = new RestClient(endpoint);
                var deviceRequest = new RestRequest(command + token);

                var response = await client.GetAsync<AlkatronicDevicesResponse>(deviceRequest);

                return response.data;
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }

            return new List<AlkatronicDevices>();
        }

        private IEnumerable<AlkatronicMeasurement> GetAlkatronicDataFromFocustronic(string endpoint, string command, AlkatronicDevicesResponse devices, string token)
        {
            try
            {
                var deviceID = devices.data
                    .Where(d => d.type == "alkatronic")
                    .Select(a => a.devices.FirstOrDefault().id)
                    .FirstOrDefault();

                // get alkatronic measurment data
                
                var client = new RestClient(endpoint);

                var alkatronicMeasurementRequest = new RestRequest(string.Format(command, deviceID));

                var alkatronicMeasurementResponse = client.Get(alkatronicMeasurementRequest);

                var measurements = JsonConvert.DeserializeObject<AlkatronicMeasurementResponse>(alkatronicMeasurementResponse.Content);

                return measurements.data.Where(d => !d.is_hidden);
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }

            return new List<AlkatronicMeasurement>();
        }

        private async Task<IEnumerable<AlkatronicMeasurement>> GetAlkatronicDataFromMeasurementsEndpoint(string endpoint, string measurementsAPIKey, CancellationToken cancelationToken)
        {
            //http request data in alkatronic

            var originalMeasurements = new List<AlkatronicMeasurement>();
            var client = new RestClient(endpoint);

            try
            {
                Log($"Requesting requesting data from Measurements Endpoint", Write: true);

                var request = new RestRequest()
                    .AddHeader("x-api-key", measurementsAPIKey);

                var response = await client.GetAsync<IEnumerable<AlkatronicMeasurement>>(request);

                Log($" => OK", header: false);

                originalMeasurements.AddRange(response);
            }
            catch (Exception ex)
            {
                Log($"ERROR: could not fetch data from Alkatronic Measurements Endpoint\r\nurl: {client.BaseUrl}\r\n\r\n");
                Log(ex.ToString());
                throw;
            }

            return originalMeasurements;
        }

        private void AddAlkatronicDataToMeasurementsEndpoint(string endpoint, IEnumerable<AlkatronicMeasurement> measurements, string measurementsAPIKey)
        {
            // post data to lambda

            var client = new RestClient(endpoint);

            try
            {
                Log($"Posting data to Measurements Endpoint", Write: true);

                var measurementsRequest = new AlkatronicMeasurementsRequest()
                {
                    Measurements = measurements,
                };

                var request = new RestRequest()
                    .AddHeader("x-api-key", measurementsAPIKey)
                    .AddJsonBody(measurements);

                var response = client.Post(request);

                var measurementsResponse = JsonConvert.DeserializeObject<dynamic>(response.Content);

                Log($" => OK", header: false);
            }
            catch (Exception ex)
            {
                Log($"ERROR: could not fetch data from Measurements Endpoint\r\nurl: {client.BaseUrl}\r\n\r\n");
                Log(ex.ToString());
                throw;
            }
        }

        #endregion
    }
}
