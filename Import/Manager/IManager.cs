using Kaenx.DataContext.Catalog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kaenx.DataContext.Import.Manager
{
    public abstract class IManager : IDisposable
    {
        public string _lanugage { get; set; } = "de-de";
        public string _path { get; set; }

        public delegate void ProcessChangedHandler(int percentage);
        public delegate void DeviceNameChanged(string newName);

        public event ProcessChangedHandler ProcessChanged;
        public event DeviceNameChanged DeviceChanged;
        public event DeviceNameChanged StateChanged;


        public IManager(string path)
        {
            _path = path;
        }


        public abstract bool CheckManager();

        /// <summary>
        /// 
        /// </summary>
        public abstract void Begin();

        /// <summary>
        /// Set lanugage so the files can be translated
        /// </summary>
        /// <param name="lang"></param>
        public void SetLanguage(string lang)
        {
            _lanugage = lang;
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
        public abstract List<ImportDevice> GetDeviceList();


        public void StartImport(string id, CatalogContext context)
        {
            StartImport(new List<string>() { id }, context);
        }
        public abstract void StartImport(List<string> ids, CatalogContext context);

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
