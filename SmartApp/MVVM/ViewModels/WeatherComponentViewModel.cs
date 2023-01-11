using SmartApp.MVVM.Cores;
using SmartApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartApp.MVVM.ViewModels
{
    internal class WeatherComponentViewModel : ObservableObject
    {
        private string? _currentWeatherCondition;
        private readonly IWeatherService _weatherService;
        private int _interval = 20000;

        public WeatherComponentViewModel(IWeatherService weatherService)
        {
            _weatherService = weatherService;
            SetWeatherAsync().ConfigureAwait(false);
        }

        public string CurrentWeatherCondition
        {
            get => _currentWeatherCondition;
            set
            {
                _currentWeatherCondition = value;
                OnPropertyChanged();
            }
        }

        private string? _currentTemperature;

        public string CurrentTemperature
        {
            get => _currentTemperature;
            set
            {
                _currentTemperature = value;
                OnPropertyChanged();
            }
        }

        private async Task SetWeatherAsync()
        {
            while (true)
            {
                var weather = await _weatherService.GetWeatherDataAsync();
                CurrentTemperature = weather.Temperature.ToString();
                CurrentWeatherCondition = weather.WeatherCondition ?? "";
                await Task.Delay(_interval);
            }
        }
    }
}
