using Kaenx.DataContext.Catalog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kaenx.DataContext.Import.Manager
{
    public abstract class IManager : IDisposable
    {
        public string _language { get; set; } = "de-de";
        public string _path { get; set; }

        public delegate void DeviceNameChanged(string newName);

        public event DeviceNameChanged DeviceChanged;
        public event DeviceNameChanged StateChanged;


        public IManager(string path)
        {
            _path = path;
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


        public void OnDeviceNameChanged(string name)
        {
            DeviceChanged?.Invoke(name);
        }

        public void OnStateChanged(string name)
        {
            StateChanged?.Invoke(name);
        }
    }
}
