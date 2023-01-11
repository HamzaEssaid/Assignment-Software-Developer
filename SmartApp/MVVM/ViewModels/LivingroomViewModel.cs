using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices;
using SmartApp.MVVM.Models;
using SmartApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SmartApp.MVVM.ViewModels
{
    internal class LivingroomViewModel
    {
        private IWeatherService _weatherService;
        private DispatcherTimer timer;
        private ObservableCollection<DeviceItem> _deviceItems;
        private List<DeviceItem> _tempList;
        private readonly RegistryManager registryManager = RegistryManager.CreateFromConnectionString("HostName=systemDev-IotHub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=X9pqkcjfVQ3jqCSRRQxSSVhgelmTEjaLnRl3ZLFfIe0=");

        public LivingroomViewModel()
        {
            _tempList = new List<DeviceItem>();
            _deviceItems = new ObservableCollection<DeviceItem>();

            _weatherService = new WeatherService();

            PopulateDeviceItemsAsync().ConfigureAwait(false);
            SetInterval(TimeSpan.FromSeconds(3));
        }


        public string Title { get; set; } = "Livingroom";

        public string Humidity { get; set; } = "34 %";
        public IEnumerable<DeviceItem> DeviceItems => _deviceItems;



        private void SetInterval(TimeSpan interval)
        {
            timer = new DispatcherTimer()
            {
                Interval = interval
            };

            timer.Tick += new EventHandler(timer_tick);
            timer.Start();
        }

        private async void timer_tick(object sender, EventArgs e)
        {
            await PopulateDeviceItemsAsync();
            await UpdateDeviceItemsAsync();
        }


        private async Task UpdateDeviceItemsAsync()
        {
            _tempList.Clear();

            foreach (var item in _deviceItems)
            {
                var device = await registryManager.GetDeviceAsync(item.DeviceId);
                if (device == null)
                    _tempList.Add(item);
            }

            foreach (var item in _tempList)
            {
                _deviceItems.Remove(item);
            }
        }

        private async Task PopulateDeviceItemsAsync()
        {
            //var result = registryManager.CreateQuery("select * from devices where location = 'bedroom'");
            var result = registryManager.CreateQuery("SELECT * FROM Devices WHERE properties.reported.location = 'livingroom'");

            if (result.HasMoreResults)
            {
                foreach (Twin twin in await result.GetNextAsTwinAsync())
                {
                    var device = _deviceItems.FirstOrDefault(x => x.DeviceId == twin.DeviceId);

                    if (device == null)
                    {
                        device = new DeviceItem
                        {
                            DeviceId = twin.DeviceId,
                        };

                        try { device.DeviceName = twin.Properties.Reported["deviceName"]; }
                        catch { device.DeviceName = device.DeviceId; }
                        try { device.DeviceType = twin.Properties.Reported["deviceType"]; }
                        catch { }

                        switch (device.DeviceType.ToLower())
                        {
                            case "fan":
                                device.IconActive = "\uf863";
                                device.IconInActive = "\uf863";
                                device.StateActive = "ON";
                                device.StateInActive = "OFF";
                                break;

                            case "light":
                                device.IconActive = "\uf672";
                                device.IconInActive = "\uf0eb";
                                device.StateActive = "ON";
                                device.StateInActive = "OFF";
                                break;

                            case "temperature":
                                device.IconActive = "\uf769";
                                device.IconInActive = "\uf2cb";
                                device.StateActive = "ON";
                                device.StateInActive = "OFF";
                                break;

                            case "aircondition":
                                device.IconActive = "\uf8f4";
                                device.IconInActive = "\uf8f4";
                                device.StateActive = "ON";
                                device.StateInActive = "OFF";
                                break;

                            default:
                                device.IconActive = "\uf2db";
                                device.IconInActive = "\uf2db";
                                device.StateActive = "ENABLE";
                                device.StateInActive = "DISABLE";
                                break;
                        }

                        _deviceItems.Add(device);
                    }
                    else { }
                }
            }
            else
            {
                _deviceItems.Clear();
            }
        }
    }
}