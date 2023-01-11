using Dapper;
using Device.WPF.Models;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Device.WPF
{
    public partial class MainWindow : Window
    {
        private readonly string _connect_url = "http://localhost:7113/api/devices/connect";
        private readonly string _connectionString = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=C:\\Users\\gongm\\source\\repos\\Assignment-Software-Developer\\Device.WPF\\Data\\device_db.mdf;Integrated Security=True;Connect Timeout=30";
        private DeviceClient _deviceClient;
        private DeviceInfo _deviceInfo;

        private string _deviceId = "";
        private bool _lightState = false;
        private bool _lightPrevState = false;
        private bool _connected = false;
        private int _interval = 3000;

        public MainWindow()
        {
            InitializeComponent();

            SetupAsync().ConfigureAwait(false);
            SendMessageAsync().ConfigureAwait(false);   
        }

        private async Task SetupAsync()
        {
            tbStateMessage.Text = "Initializing device. Please wait...";

            using IDbConnection conn = new SqlConnection(_connectionString);
            _deviceId = await conn.QueryFirstOrDefaultAsync<string>("SELECT DeviceId FROM DeviceInfo");
            if (string.IsNullOrEmpty(_deviceId))
            {
                tbStateMessage.Text = "Generating new DeviceID";
                _deviceId = Guid.NewGuid().ToString();
                await conn.ExecuteAsync("INSERT INTO DeviceInfo (DeviceId,DeviceName,DeviceType,Location,Owner) VALUES (@DeviceId, @DeviceName, @DeviceType, @Location, @Owner)", new { DeviceId = _deviceId, DeviceName = "WPF DeviceLight", DeviceType = "light", Location = "kitchen", Owner = "Gong Moonphruk" });
            }

            var device_ConnectionString = await conn.QueryFirstOrDefaultAsync<string>("SELECT ConnectionString FROM DeviceInfo WHERE DeviceId = @DeviceId", new { DeviceId = _deviceId });

            if (string.IsNullOrEmpty(device_ConnectionString))
            {
                tbStateMessage.Text = "Initializing connectionstring. Please wait...";

                await Task.Delay(5000);
                using var http = new HttpClient();
                var result = await http.PostAsJsonAsync(_connect_url, new { DeviceId = _deviceId });
                device_ConnectionString = await result.Content.ReadAsStringAsync();
                await conn.ExecuteAsync("UPDATE DeviceInfo SET ConnectionString = @ConnectionString WHERE DeviceId = @DeviceId", new { DeviceId = _deviceId, ConnectionString = device_ConnectionString });
            }

            _deviceClient = DeviceClient.CreateFromConnectionString(device_ConnectionString);

            tbStateMessage.Text = "Updating Twin Properties. Please wait...";

            _deviceInfo = await conn.QueryFirstOrDefaultAsync<DeviceInfo>("SELECT * FROM DeviceInfo WHERE DeviceId = @DeviceId", new { DeviceId = _deviceId });

            // Set twin properties
            var twinCollection = new TwinCollection();
            twinCollection["deviceName"] = _deviceInfo.DeviceName;
            twinCollection["deviceType"] = _deviceInfo.DeviceType;
            twinCollection["location"] = _deviceInfo.Location;
            twinCollection["owner"] = _deviceInfo.Owner;
            twinCollection["lightState"] = _lightState;

            await _deviceClient.UpdateReportedPropertiesAsync(twinCollection);

            _connected = true;

            tbStateMessage.Text = "Device Connected.";
        }

        private async Task SendMessageAsync()
        {
            while (true)
            {
                if (_connected)
                {
                    if (_lightState != _lightPrevState)
                    {
                        _lightPrevState = _lightState;

                        // Device to Cloud (d2c)
                        var json = JsonConvert.SerializeObject(new { lightState = _lightState });
                        var message = new Message(Encoding.UTF8.GetBytes(json));

                        message.Properties.Add("deviceName", _deviceInfo.DeviceName);
                        message.Properties.Add("deviceType", _deviceInfo.DeviceType);
                        message.Properties.Add("location", _deviceInfo.Location);
                        message.Properties.Add("owner", _deviceInfo.Owner);

                        await _deviceClient.SendEventAsync(message);
                        tbStateMessage.Text = $"Message sent at {DateTime.Now}";

                        // Device twin (reported)
                        var twinCollection = new TwinCollection();
                        twinCollection["lightState"] = _lightState;
                        await _deviceClient.UpdateReportedPropertiesAsync(twinCollection);
                    }
                }

                await Task.Delay(_interval);

            }
        }

        private void btnOnOff_Click(object sender, RoutedEventArgs e)
        {
            _lightState = !_lightPrevState;

            if (_lightState)
                btnOnOff.Content = "Turn OFF";
            else
                btnOnOff.Content = "Turn ON";
        }
    }
}
