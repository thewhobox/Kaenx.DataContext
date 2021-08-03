using Kaenx.DataContext.Catalog;
using Kaenx.Import.Dynamic;
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

                    //TODO add Application


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
                    ChannelIndependentBlock channel = new ChannelIndependentBlock();
                    Channels.Add(channel);


                    OnStateChanged("Parameter");

                    foreach(XElement group in xdevice.Element(GetXName("Parameters")).Elements(GetXName("ParameterGroup")))
                    {
                        ParameterBlock block = new ParameterBlock();
                        block.DisplayText = group.Attribute("Name").Value;

                        foreach(XElement xpara in group.Elements())
                        {
                            //TODO check if para exists
                            AppParameter para = new AppParameter();
                            para.Access = AccessType.Full;
                            para.ParameterId = int.Parse(xpara.Attribute("Id").Value);
                            para.Text = xpara.Element(GetXName("Description")).Value;

                            XElement xvalue = xpara.Element(GetXName("Value"));
                            para.Value = HexToString(xvalue.Attribute("Default").Value);

                            if (xvalue.Attribute("Options").Value.Contains("|"))
                            {
                                string[] options = xvalue.Attribute("Options").Value.Split('|');

                                if(options.Length == 2)
                                {
                                    para.ParameterTypeId = -1; //TODO add ParamTypes

                                    ParamEnumTwo pet = new ParamEnumTwo() { Text = para.Text };
                                    string[] opt1 = options[0].Split('=');
                                    pet.Option1 = new ParamEnumOption() { Text = opt1[1], Value = HexToString(opt1[0]) };
                                    string[] opt2 = options[1].Split('=');
                                    pet.Option2 = new ParamEnumOption() { Text = opt2[1], Value = HexToString(opt2[0]) };
                                    block.Parameters.Add(pet);
                                } else
                                {
                                    para.ParameterTypeId = -1;

                                    ParamEnum pe = new ParamEnum() { Text = para.Text };
                                    foreach (string option in xvalue.Attribute("Options").Value.Split('|'))
                                    {
                                        string[] opts = option.Split('=');
                                        ParamEnumOption opt = new ParamEnumOption();
                                        opt.Text = opts[1];
                                        opt.Value = HexToString(opts[0]);
                                        pe.Options.Add(opt);
                                    }
                                    block.Parameters.Add(pe);
                                }
                            }
                        }


                        channel.Blocks.Add(block);
                    }

                }
            }

            context.SaveChanges();

            OnDeviceNameChanged("Fertig");
            OnStateChanged("Abgeschlossen");
        }

        private string HexToString(string input)
        {
            return int.Parse(input, System.Globalization.NumberStyles.HexNumber).ToString();
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
