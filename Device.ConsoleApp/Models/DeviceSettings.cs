using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Device.ConsoleApp.Models
{
    internal class DeviceSettings
    {
        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; } = null!;

        [JsonPropertyName("connectionString")]
        public string ConnectionString { get; set; } = null!;
       
        [JsonPropertyName("deviceName")]
        public string DeviceName { get; set; } = null!;

        [JsonPropertyName("deviceType")]
        public string DeviceType { get; set; } = null!;

        [JsonPropertyName("location")]
        public string Location { get; set; } = null!;

        [JsonPropertyName("interval")]
        public int Interval { get; set; } = 10000; 

        [JsonPropertyName("deviceState")]
        public bool DeviceState { get; set; } = false; 

        //k
    }
}
