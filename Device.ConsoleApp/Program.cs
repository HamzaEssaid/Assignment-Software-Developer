using Device.ConsoleApp.Models;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System.Net.Http.Json;
using System.Text;
using static Dapper.SqlMapper;

namespace Device.ConsoleApp;

class Program
{
    private static Twin _twin;
    private static DeviceClient _deviceClient;
    private static DeviceSettings _settings = new DeviceSettings();
    private static string _apiUri = "http://localhost:7113/api/devices/connect";
    private static string _filePath = @$"{AppDomain.CurrentDomain.BaseDirectory}\configuration.json";

    public static async Task Main()
    {
        await GetConfigurationAsync();

        if (string.IsNullOrEmpty(_settings.DeviceId))
            SetSettings();

        await SetConnectionStringAsync();
        await InitializeDeviceAsync();
        await SetIntervalAsync();
        await SetDeviceTwinAsync();
        await SetDirectMethodAsync();
        await SaveConfigurationAsync();

        Console.Clear();
        Console.WriteLine($"Device {_settings.DeviceId} connected/configured and awaiting new commands.");
        Console.ReadKey();
    }

    private static async Task GetConfigurationAsync()
    {
        try
        {
            using var sr = new StreamReader(_filePath);
            _settings = JsonConvert.DeserializeObject<DeviceSettings>(await sr.ReadToEndAsync());
        }
        catch { }
    }

    private static async Task SaveConfigurationAsync()
    {
        try
        {
            using var sw = new StreamWriter(_filePath);
            await sw.WriteLineAsync(JsonConvert.SerializeObject(_settings));
        }
        catch { }
    }

    private static void SetSettings()
    {
        Console.Clear();
        Console.WriteLine("##### Device Settings Configuration #####\n");

        _settings.DeviceId = Guid.NewGuid().ToString();
        Console.Write($"DeviceId: {_settings.DeviceId}\n");

        Console.Write("Enter Device Name: ");
        _settings.DeviceName = Console.ReadLine() ?? "";

        Console.Write("Enter Device Type: ");
        _settings.DeviceType = Console.ReadLine() ?? "";

        Console.Write("Enter Location: ");
        _settings.Location = Console.ReadLine() ?? "";

        Console.WriteLine("\n");
    }

    private static async Task SetConnectionStringAsync()
    {
        Console.Write("\nConfiguring connectionString. Please wait...");

        using var client = new HttpClient();
        while (string.IsNullOrEmpty(_settings.ConnectionString))
        {
            Console.Write(".");

            try
            {
                var result = await client.PostAsJsonAsync(_apiUri, _settings);
                if (result.IsSuccessStatusCode)
                    _settings.ConnectionString = await result.Content.ReadAsStringAsync();
            }
            catch
            {
                await Task.Delay(500);
            }
        }
    }

    private static async Task InitializeDeviceAsync()
    {
        Console.Write($"\nInitializing device {_settings.DeviceId}. Please wait...");

        bool isConfigured = false;

        while (!isConfigured)
        {
            Console.Write(".");

            try
            {
                _deviceClient = DeviceClient.CreateFromConnectionString(_settings.ConnectionString, TransportType.Mqtt);
                _twin = await _deviceClient.GetTwinAsync();
            }
            catch { }

            if (_deviceClient != null && _twin != null)
                isConfigured = true;

            await Task.Delay(500);
        }
    }

    private static async Task SetIntervalAsync()
    {
        Console.Write($"\nConfiguring sending interval. Please wait...");

        try { _settings.Interval = (int)_twin.Properties.Desired["interval"]; }
        catch
        {
            Console.Write(" - Failed! No interval property found.");
        }
        await Task.Delay(500);
    }


    private static async Task SetDeviceTwinAsync()
    {
        Console.WriteLine("Configuring DeviceTwin Properties. Please wait...");

        var twinCollection = new TwinCollection();
        twinCollection["deviceName"] = _settings.DeviceName;
        twinCollection["deviceType"] = _settings.DeviceType;
        twinCollection["deviceState"] = _settings.DeviceState;
        twinCollection["location"] = _settings.Location;

        await _deviceClient.UpdateReportedPropertiesAsync(twinCollection);

    }

    private static async Task SetDirectMethodAsync()
    {
        Console.WriteLine("Configuring Direct Method (ON/OFF). Please wait...");
        await _deviceClient.SetMethodHandlerAsync("OnOff", OnOff, null);
    }

    private static async Task<MethodResponse> OnOff(MethodRequest methodRequest, object userContext)
    {
        try
        {
            var data = JsonConvert.DeserializeObject<dynamic>(methodRequest.DataAsJson);
            Console.WriteLine($"Changing DeviceState from {_settings.DeviceState} to {data!.deviceState}.");
            _settings.DeviceState = data!.deviceState;

            SetDeviceTwinAsync().ConfigureAwait(false);
            SaveConfigurationAsync().ConfigureAwait(false);
            Console.WriteLine($"Device {_settings.DeviceId} configured and awaiting new commands.");
            return await Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(_settings)), 200));
        }
        catch (Exception ex)
        {
            return await Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ex)), 400));
        }
    }

    private static async Task GetWeatherData()
    {
        var temperature = 0.0;
        var humidity = 0.0;
    }

    private static async Task SendDataAsync()
    {
        var temperature = 0.0;
        var humidity = 0.0;
        var json = JsonConvert.SerializeObject(new { temperature, humidity });
        var message = new Message(Encoding.UTF8.GetBytes(json));
    }

}