using Kaenx.DataContext.Catalog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Kaenx.DataContext.Import.Manager
{
    public class Konnecting : IManager
    {

        private XElement kDevice { get; set; }



        public Konnecting(string path) : base(path) { }
        
        public override bool CheckManager()
        {
            return File.Exists(_path) && _path.EndsWith(".kdevice.xml");
        }


        public override void Begin()
        {
            kDevice = XDocument.Load(_path).Root;
        }

        public override List<string> GetLanguages()
        {
            return new List<string>() { "de-de" };
        }

        public override List<ImportDevice> GetDeviceList()
        {
            List<ImportDevice> devices = new List<ImportDevice>();


            foreach(XElement xdev in kDevice.Descendants(GetXName("Device")))
            {
                ImportDevice device = new ImportDevice()
                {
                    Id = xdev.Attribute("DeviceId").Value
                };
                device.Name = xdev.Element(GetXName("DeviceName")).Value;

                devices.Add(device);
            }


            return devices;
        }

        public override void StartImport(List<string> ids, CatalogContext context)
        {
            XElement devices = kDevice.Element(GetXName("Device")).Element(GetXName("Parameters"));

            foreach(XElement xdevice in kDevice.Elements(GetXName("Device")))
            {
                if (ids.Contains(xdevice.Attribute("DeviceId").Value))
                {
                    string manuName = xdevice.Element(GetXName("ManufacturerName")).Value;
                    int hardNumber = int.Parse(xdevice.Attribute("DeviceId").Value);
                    int hardVersion = int.Parse(xdevice.Attribute("Revision").Value);
                    bool sectionExists = context.Sections.Any(s => s.ParentId == -1 && s.ImportType == ImportTypes.Konnekting && s.Name == manuName);
                    bool hardwareExists = context.Hardware2App.Any(s => s.Number == hardNumber && s.Version == hardNumber);
                    int sectionId = 0;
                    int hardwareId = 0;

                    if (sectionExists)
                    {
                        CatalogViewModel section = context.Sections.Single(s => s.ParentId == -1 && s.ImportType == ImportTypes.Konnekting && s.Name == manuName);
                        sectionId = section.Id;
                    } else
                    {
                        CatalogViewModel section = new CatalogViewModel()
                        {
                            ParentId = -1,
                            Name = manuName,
                            ImportType = ImportTypes.Konnekting
                        };
                        context.Sections.Add(section);
                        context.SaveChanges();
                        sectionId = section.Id;
                    }

                    if (hardwareExists)
                    {
                        Hardware2AppModel hard = context.Hardware2App.Single(s => s.Number == hardNumber && s.Version == hardNumber);
                        hardwareId = hard.Id;
                    } else
                    {
                        Hardware2AppModel hard = new Hardware2AppModel()
                        {
                            Number = hardNumber,
                            Version = hardVersion
                        };
                        context.Hardware2App.Add(hard);
                        context.SaveChanges();
                        hardwareId = hard.Id;
                    }


                    DeviceViewModel model = new DeviceViewModel();
                    model.Name = xdevice.Element(GetXName("DeviceName")).Value;
                    model.HasIndividualAddress = true;
                    model.HasApplicationProgram = true;
                    model.IsPowerSupply = false;
                    model.IsCoupler = false;
                    model.CatalogId = sectionId;
                    model.HardwareId = hardwareId;
                    context.Devices.Add(model);


                    AppAdditional adds = new AppAdditional();

                    List<IDynChannel> Channels = new List<IDynChannel>();

                    OnStateChanged("Parameter");

                    foreach(XElement group in xdevice.Element(GetXName("Parameters")).Elements(GetXName("ParameterGroup")))
                    {

                    }

                }
            }

            context.SaveChanges();

            OnDeviceNameChanged("Fertig");
            OnStateChanged("Abgeschlossen");
        }

        private XName GetXName(string name)
        {
            return XName.Get(name, kDevice.Name.NamespaceName);
        }

        public override void Dispose()
        {
            //do nothing
        }
    }
}
