using DeltaComparer;
using Newtonsoft.Json;
using Quartz;
using RestSharp;
using RestSharp.Serialization.Json;
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
                await Log($"ERROR: Missing configuration file: {configFile}", skipLog: true);
            }

            var configContents = File.ReadAllText(configFile);

            var config = JsonConvert.DeserializeObject<dynamic>(configContents);

            //var mastertronicLocalIP = config.mastertronicLocalIP.Value;
            var mastertronicCommand = config.mastertronicCommand.Value as string;
            var alkatronicEndpoint = config.alkatronicEndpoint.Value as string;
            var alkatronicMeasurementsCommand = config.alkatronicMeasurementsCommand.Value as string;
            var alkatronicDevicesCommand = config.alkatronicDevicesCommand.Value as string;
            var alkatronicLoginCommand = config.alkatronicLoginCommand.Value as string;
            var alkatronicEmail = config.alkatronicEmail.Value as string;
            var alkatronicPassword = config.alkatronicPassword.Value as string;
            var measurementsBaseEndpoint = config.measurementsBaseEndpoint.Value as string;
            var measurementsEndpoint = config.measurementsEndpoint.Value as string;
            var measurementsAlkatronicEndpoint = config.measurementsAlkatronicEndpoint.Value as string;
            var measurementsAPIKey = config.measurementsAPIKey.Value as string;
            var measurementsCommand = config.measurementsCommand.Value as string;
            var logging = config.logging.Value as string;
            var logFile = config.logFile.Value as string;
            var parameters = config.parameters.ToObject<List<string>>() as List<string>; // todo: validate parameter values

            if (string.IsNullOrEmpty(alkatronicEndpoint))
                await Log($"ERROR: could not find alkatronicEndpoint in config file", skipLog: true);
            if (string.IsNullOrEmpty(alkatronicMeasurementsCommand))
                await Log($"ERROR: could not find alkatronicMeasurementsCommand in config file", skipLog: true);
            if (string.IsNullOrEmpty(alkatronicDevicesCommand))
                await Log($"ERROR: could not find alkatronicDevicesCommand in config file", skipLog: true);
            if (string.IsNullOrEmpty(mastertronicCommand))
                await Log($"ERROR: could not find mastertronicCommand in config file", skipLog: true);
            if (string.IsNullOrEmpty(logging))
                await Log($"ERROR: could not find logging in config file", skipLog: true);
            if (string.IsNullOrEmpty(logFile))
                await Log($"ERROR: could not find logFile in config file", skipLog: true);
            if (!parameters.Any())
                await Log($"ERROR: could not find parameters in config file", skipLog: true);
            if (string.IsNullOrEmpty(measurementsBaseEndpoint))
                await Log($"ERROR: could not find measurementsBaseEndpoint in config file", skipLog: true);
            if (string.IsNullOrEmpty(measurementsEndpoint))
                await Log($"ERROR: could not find measurementsEndpoint in config file", skipLog: true);
            if (string.IsNullOrEmpty(measurementsAlkatronicEndpoint))
                await Log($"ERROR: could not find measurementsAlkatronicEndpoint in config file", skipLog: true);
            if (string.IsNullOrEmpty(measurementsAPIKey))
                await Log($"ERROR: could not find measurementsAPIKey in config file", skipLog: true);
            if (string.IsNullOrEmpty(measurementsCommand))
                await Log($"ERROR: could not find measurementsCommand in config file", skipLog: true);

            if (logging == "true")
            {
                log = true;
                logfile = logFile;
            }


            await Log("Logging in to Focustronic/Alkatronic", true);

            var loginResponse = await LoginToAlkatronic(alkatronicEndpoint + alkatronicLoginCommand, alkatronicEmail, alkatronicPassword) as AlkatronicLoginReponse;

            await Log("=> OK", header: false);

            var devices = await GetDevicesFromFocustronic(alkatronicEndpoint, alkatronicDevicesCommand, loginResponse.data) as IEnumerable<AlkatronicDevices>;

            await Log("Searching for Alkatronic");

            // validate there is an alkatronic
            var alkatronic = devices.Where(ad => ad.type == "alkatronic")
                                    .FirstOrDefault()?
                                    .devices?
                                    .FirstOrDefault() ?? null;

            if (alkatronic != null)
            {
                try
                {
                    await Log("Starting data collection for Alkatronic");

                    var deviceID = alkatronic.id;

                    var originalAlkatronicMeasurements = await GetAlkatronicDataFromMeasurementsEndpoint(measurementsBaseEndpoint + measurementsAlkatronicEndpoint + "?days=7", measurementsAPIKey, context.CancellationToken) as IEnumerable<AlkatronicMeasurement>;

                    var modifiedAlkatronicMeasurements = await GetAlkatronicDataFromFocustronic(alkatronicEndpoint, alkatronicMeasurementsCommand, deviceID, loginResponse.data) as IEnumerable<AlkatronicMeasurement>;

                    // delta results / hash
                    var alkaDeltaResults = Delta.Compare(
                        modifieds: modifiedAlkatronicMeasurements,
                        originals: originalAlkatronicMeasurements,
                        m => m.record_time);

                    if (alkaDeltaResults.Adds.Any())
                    {
                        await AddAlkatronicDataToMeasurementsEndpoint(measurementsBaseEndpoint + measurementsAlkatronicEndpoint, alkaDeltaResults.Adds, measurementsAPIKey);
                    }

                    await Log("Done");
                }
                catch(Exception ex)
                {
                    await Log(ex.ToString());
                }
            }


            await Log("Searching for Mastertronic");

            // get the local ip of the mastertronic
            var mastertronicLocalIP = devices.Where(ad => ad.type == "mastertronic")
                                             .Select(ad => ad.devices.FirstOrDefault().local_ip_address)
                                             .FirstOrDefault() ?? string.Empty;

            if (!string.IsNullOrEmpty(mastertronicLocalIP))
            {
                try
                {
                    await Log("Starting data collection for Mastertronic");

                    var originalMastertronicMeasurements = await GetMastertronicDataFromMeasurementsEndpoint(measurementsBaseEndpoint + measurementsEndpoint, measurementsAPIKey) as IEnumerable<Measurement>;

                    var modifiedMastertronicMeasurements = await GetMastertronicDataFromMastertronic(mastertronicLocalIP, mastertronicCommand, parameters) as IEnumerable<Measurement>;

                    // delta results / hash
                    var masterDeltaResults = Delta.Compare(
                        modifieds: modifiedMastertronicMeasurements,
                        originals: originalMastertronicMeasurements,
                        m => m.record_time);

                    if (masterDeltaResults.Adds.Any())
                    {
                        await AddMastertronicDataToMeasurementsEndpoint(measurementsBaseEndpoint + measurementsEndpoint, masterDeltaResults.Adds, measurementsAPIKey);
                    }

                    await Log("Done");
                }
                catch(Exception ex)
                {
                    await Log(ex.ToString());
                }
            }
        }

        private async Task Log(string message, bool Write = false, bool skipLog = false, bool header = true)
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
                        await writer.WriteAsync(message);
                    else
                        await writer.WriteLineAsync(message);
                }
        }

        #region Mastertronic

        private async Task<IEnumerable<Measurement>> GetMastertronicDataFromMastertronic(string ip, string command, IEnumerable<string> parameters)
        {
            //http request data in mastertronic

            var modifiedMeasurements = new List<Measurement>();

            var localURL = $"http://{ip}/";
            var client = new RestClient(localURL);

            client.AddHandler("text/plain", () => { return new RestSharp.Serialization.Json.JsonSerializer(); });

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

                    await Log($"Requesting {parameter} from Mastertronic", Write: true);

                    var request = new RestRequest($"{command}{DateTime.UtcNow.Ticks}", DataFormat.None)
                        .AddJsonBody(mastertronicRequest);

                    var response = await client.PostAsync<MastertronicResponse>(request);

                    await Log($" => OK", header: false);

                    modifiedMeasurements.AddRange(response.data);
                }
                catch (Exception ex)
                {
                    await Log($"ERROR: could not fetch data from MT\r\nparam: {parameter}, url: {client.BaseUrl}\r\n\r\n");
                    await Log(ex.ToString());
                    throw;
                }
            }

            return modifiedMeasurements;
        }

        private async Task<IEnumerable<Measurement>> GetMastertronicDataFromMeasurementsEndpoint(string measurementsEndpoint, string measurementsAPIKey)
        {
            //http request data in mastertronic

            var originalMeasurements = new List<Measurement>();
            var client = new RestClient(measurementsEndpoint);

            try
            {
                await Log($"Requesting requesting data from Measurements Endpoint", Write: true);

                var request = new RestRequest()
                    .AddHeader("x-api-key", measurementsAPIKey);
                
                var response = await client.GetAsync<IEnumerable<Measurement>>(request);

                await Log($" => OK", header: false);

                originalMeasurements.AddRange(response);
            }
            catch (Exception ex)
            {
                await Log($"ERROR: could not fetch data from Measurements Endpoint\r\nurl: {client.BaseUrl}\r\n\r\n");
                await Log(ex.ToString());
                throw;
            }

            return originalMeasurements;
        }

        private async Task AddMastertronicDataToMeasurementsEndpoint(string endpoint, IEnumerable<Measurement> measurements, string measurementsAPIKey)
        {
            // post data to lambda

            var client = new RestClient(endpoint);

            try
            {
                await Log($"Posting data to Measurements Endpoint", Write: true);

                var measurementsRequest = new MeasurementsRequest()
                {
                    Measurements = measurements,
                };

                var request = new RestRequest()
                    .AddHeader("x-api-key", measurementsAPIKey)
                    .AddJsonBody(measurements);

                await client.PostAsync<dynamic>(request);

                await Log($" => OK", header: false);
            }
            catch (Exception ex)
            {
                await Log($"ERROR: could not fetch data from Measurements Endpoint\r\nurl: {client.BaseUrl}\r\n\r\n");
                await Log(ex.ToString());
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
                await Log(ex.ToString());
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
                await Log(ex.ToString());
            }

            return new List<AlkatronicDevices>();
        }

        private async Task<IEnumerable<AlkatronicMeasurement>> GetAlkatronicDataFromFocustronic(string endpoint, string command, int deviceID, string token)
        {
            try
            {
                // get alkatronic measurment data
                
                var client = new RestClient(endpoint);

                var alkatronicMeasurementRequest = new RestRequest(string.Format(command, deviceID));

                var alkatronicMeasurementResponse = await client.GetAsync<AlkatronicMeasurementResponse>(alkatronicMeasurementRequest);

                return alkatronicMeasurementResponse.data.Where(d => !d.is_hidden);
            }
            catch (Exception ex)
            {
                await Log(ex.ToString());
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
                await Log($"Requesting requesting data from Measurements Endpoint", Write: true);

                var request = new RestRequest()
                    .AddHeader("x-api-key", measurementsAPIKey);

                var response = await client.GetAsync<IEnumerable<AlkatronicMeasurement>>(request);

                await Log($" => OK", header: false);

                originalMeasurements.AddRange(response);
            }
            catch (Exception ex)
            {
                await Log($"ERROR: could not fetch data from Alkatronic Measurements Endpoint\r\nurl: {client.BaseUrl}\r\n\r\n");
                await Log(ex.ToString());
                throw;
            }

            return originalMeasurements;
        }

        private async Task AddAlkatronicDataToMeasurementsEndpoint(string endpoint, IEnumerable<AlkatronicMeasurement> measurements, string measurementsAPIKey)
        {
            // post data to lambda

            var client = new RestClient(endpoint);

            try
            {
                await Log($"Posting data to Measurements Endpoint", Write: true);

                var measurementsRequest = new AlkatronicMeasurementsRequest()
                {
                    Measurements = measurements,
                };

                var request = new RestRequest()
                    .AddHeader("x-api-key", measurementsAPIKey)
                    .AddJsonBody(measurements);

                await client.PostAsync<dynamic>(request);

                await Log($" => OK", header: false);
            }
            catch (Exception ex)
            {
                await Log($"ERROR: could not add data to Measurements Endpoint\r\nurl: {client.BaseUrl}\r\n\r\n");
                await Log(ex.ToString());
                throw;
            }
        }

        #endregion
    }
}
