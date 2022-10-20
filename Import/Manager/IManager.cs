using Kaenx.DataContext.Catalog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Kaenx.DataContext.Import.Manager
{
    public abstract class IManager : IDisposable, INotifyPropertyChanged
    {
        public string _language { get; set; } = "de-de";
        public string _path { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public delegate void DeviceNameChanged(string newName);

        public event DeviceNameChanged DeviceChanged;
        public event DeviceNameChanged StateChanged;

        private int _importSteps = 0;

        private double _progressValue = 0;
        public double ProgressValue {
            get { return _progressValue; }
            set { _progressValue = value; Changed("ProgressValue"); }
        }

        private string _currentDevice = null;
        public string CurrentDevice {
            get { return _currentDevice; }
            set { _currentDevice = value; Changed("CurrentDevice"); DeviceChanged?.Invoke(value); ProgressDeviceChanged(); }
        }

        private string _currentState = null;
        public string CurrentState {
            get { return _currentState; }
            set { _currentState = value; Changed("CurrentState"); StateChanged?.Invoke(value); ProgressStateChanged(); }
        }

        private double deviceStep;
        private int deviceCount;
        private double stateStep;
        private int stateCount;

        public void ProgressCalculate(int count)
        {
            deviceCount = -1;
            deviceStep = 1.0 / count;
            stateCount = -1;
            stateStep = deviceStep / _importSteps;
        }

        private void ProgressDeviceChanged()
        {
            deviceCount++;
            ProgressValue = deviceStep * deviceCount;
        }

        private void ProgressStateChanged()
        {
            ProgressValue = (deviceStep * deviceCount) + (stateStep * stateCount);
        }

        public IManager(string path, int steps)
        {
            _path = path;
            _importSteps = steps;
        }

        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public abstract bool CheckManager();

        /// <summary>
        /// Set lanugage so the files can be translated
        /// </summary>
        /// <param name="lang"></param>
        public void SetLanguage(string lang)
        {
            _language = lang;
        }

        /// <summary>
        /// Returns all supported languages
        /// </summary>
        /// <returns></returns>
        public abstract List<string> GetLanguages();

        /// <summary>
        /// Returns device list exportet from the file
        /// </summary>
        /// <returns>List of devices</returns>
        public abstract List<ImportDevice> GetDeviceList(CatalogContext context = null);


        public void StartImport(ImportDevice device, CatalogContext context)
        {
            StartImport(new List<ImportDevice>() { device }, context);
        }
        public abstract void StartImport(List<ImportDevice> devices, CatalogContext context);

        public abstract void Dispose();
    }
}
