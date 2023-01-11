using IoTHubTrigger = Microsoft.Azure.WebJobs.EventHubTriggerAttribute;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.EventHubs;
using System.Text;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzureFunctions_API
{
    public class SaveData
    {
        private static HttpClient client = new HttpClient();
        
        [FunctionName("SaveData")]
        public void Run([IoTHubTrigger("messages/events", Connection = "IotHubEndpoint")] EventData message,
            [CosmosDB(databaseName: "NET", collectionName: "Data", CreateIfNotExists = true, ConnectionStringSetting = "CosmosDB")] out dynamic output,
            ILogger log)
            
        {
            try
            {
                output = new
                {
                    deviceId = message.SystemProperties["iothub-connection-device-id"].ToString(),
                    deviceName = message.Properties["deviceName"].ToString(),
                    deviceType = message.Properties["deviceType"].ToString(),
                    owner = message.Properties["owner"].ToString(),
                    data = JsonConvert.DeserializeObject<dynamic>(Encoding.UTF8.GetString(message.Body.Array))
                };
            }
            catch 
            {
                output = null;
            }
        }
    }
}