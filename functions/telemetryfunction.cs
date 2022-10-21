using Azure;
using Azure.Core.Pipeline;
using Azure.DigitalTwins.Core;
using Azure.Identity;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Collections.Generic;

namespace My.Function
{
    // This class processes telemetry events from IoT Hub, reads temperature of a device
    // and sets the "Temperature" property of the device with the value of the telemetry.
    public class telemetryfunction
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static string adtServiceUrl = Environment.GetEnvironmentVariable("ADT_SERVICE_URL");

        [FunctionName("telemetryfunction")]
        public async void Run([EventGridTrigger] EventGridEvent eventGridEvent, ILogger log)
        {
            try
            {
                // After this is deployed, you need to turn the Managed Identity Status to "On",
                // Grab Object Id of the function and assigned "Azure Digital Twins Owner (Preview)" role
                // to this function identity in order for this function to be authorized on ADT APIs.
                //Authenticate with Digital Twins
                var credentials = new DefaultAzureCredential();
                log.LogInformation(credentials.ToString());
                DigitalTwinsClient client = new DigitalTwinsClient(
                    new Uri(adtServiceUrl), credentials, new DigitalTwinsClientOptions
                    { Transport = new HttpClientTransport(httpClient) });
                log.LogInformation($"ADT service client connection created.");
                
                if (eventGridEvent != null && eventGridEvent.Data != null)
                {
                    JObject deviceMessage = (JObject)JsonConvert.DeserializeObject(eventGridEvent.Data.ToString());
                    string deviceId = (string)deviceMessage["systemProperties"]["iothub-connection-device-id"];
                    var ID = deviceMessage["body"]["autoid"];
                    var TimeInterval = deviceMessage["body"]["TimeInterval"];
                    var Car1_Acceleration = deviceMessage["body"]["Car1_Acceleration"];
                    var Car1_Velocity = deviceMessage["body"]["Car1_Velocity"];
                    var Car1_BrakeForce = deviceMessage["body"]["Car1_BrakeForce"];
                    var Car2_Acceleration = deviceMessage["body"]["Car2_Acceleration"];
                    var Car2_Velocity = deviceMessage["body"]["Car2_Velocity"];
                    var Car2_BrakeForce = deviceMessage["body"]["Car2_BrakeForce"];
                    var DistanceBetweenCars = deviceMessage["body"]["DistanceBetweenCars"];
                    var isCrash = deviceMessage["body"]["isCrash"];

                    log.LogInformation($"Device:{deviceId} Device Id is:{ID}");
                    log.LogInformation($"Device:{deviceId} Time interval is:{TimeInterval}");
                    log.LogInformation($"Device:{deviceId} Car1_Acceleration is:{Car1_Acceleration}");
                    log.LogInformation($"Device:{deviceId} Car1_Velocity is:{Car1_Velocity}");
                    log.LogInformation($"Device:{deviceId} Car1_BrakeForce is:{Car1_BrakeForce}");
                    log.LogInformation($"Device:{deviceId} Car2_Acceleration is:{Car2_Acceleration}");
                    log.LogInformation($"Device:{deviceId} Car2_Velocity:{Car2_Velocity}");
                    log.LogInformation($"Device:{deviceId} Car2_BrakeForce is:{Car2_BrakeForce}");
                    log.LogInformation($"Device:{deviceId} DistanceBetweenCars is:{DistanceBetweenCars}");
                    log.LogInformation($"Device:{deviceId} isCrash is:{isCrash}");
                    var updateProperty = new JsonPatchDocument();
                    var turbineTelemetry = new Dictionary<string, Object>()
                    {
                        ["autoid"] = ID,
                        ["TimeInterval"] = TimeInterval,
                        ["Car1_Acceleration"] = Car1_Acceleration,
                        ["Car1_Velocity"] = Car1_Velocity,
                        ["Car1_BrakeForce"] = Car1_BrakeForce,
                        ["Car2_Acceleration"] = Car2_Acceleration,
                        ["Car2_Velocity"] = Car2_Velocity,
                        ["Car2_BrakeForce"] = Car2_BrakeForce,
                        ["DistanceBetweenCars"] = DistanceBetweenCars,
                        ["isCrash"] = isCrash
                    };
                    updateProperty.AppendAdd("/autoid", ID.Value<string>());

                    log.LogInformation(updateProperty.ToString());
                    try
                    {
                        await client.PublishTelemetryAsync(deviceId, Guid.NewGuid().ToString(), JsonConvert.SerializeObject(turbineTelemetry));
                    }
                    catch (Exception e)
                    {
                        log.LogInformation(e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                log.LogInformation(e.Message);
            }
        }
    }
}