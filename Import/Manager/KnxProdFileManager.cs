using Kaenx.DataContext.Import.Dynamic;
using Kaenx.DataContext.Import.Values;
using Kaenx.DataContext.Catalog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Diagnostics;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;

namespace Kaenx.DataContext.Import.Manager
{
    public class KnxProdFileManager : IManager, IDisposable
    {
        private ZipArchive Archive { get; set; }
        private CatalogContext _context {get;set;}
        private string currentNamespace {get;set;}
        private string appName {get;set;}

        private Dictionary<int, List<List<ParamCondition>>> ComConditions;
        private List<ParamBinding> Bindings;
        private Dictionary<int, AppParameter> AppParas;
        private Dictionary<int, AppParameterTypeViewModel> AppParaTypes;
        private Dictionary<int, AppComObject> ComObjects;
        private List<ComBinding> ComBindings;
        private List<AssignParameter> Assignments;
        private Dictionary<string, int> parameterTypeIds;
        private int counterUnion = 1;
        private int counterParameter = 1;
        private int counterComObjects = 0;


        public KnxProdFileManager(string path) : base(path, 8) { }

        public override bool CheckManager()
        {
            return File.Exists(_path) && _path.ToLower().EndsWith(".knxprod");
        }


        public override List<string> GetLanguages()
        {
            if(Archive == null)
                Archive = ZipFile.OpenRead(_path);

            List<string> langs = new List<string>();

            foreach (ZipArchiveEntry entryTemp in Archive.Entries)
            {
                if (entryTemp.FullName.Contains("_A-") ||
                    entryTemp.FullName.EndsWith("Catalog.xml") ||
                    entryTemp.FullName.EndsWith("Hardware.xml"))
                {
                    using (Stream entryStream = entryTemp.Open())
                    {
                        XDocument catXML = XDocument.Load(entryStream);
                        string ns = catXML.Root.Name.NamespaceName;

                        if (entryTemp.FullName.Contains("_A-"))
                        {
                            XElement xapp = catXML.Descendants(XName.Get("ApplicationProgram", ns)).ElementAt(0);
                            string defLang = xapp.Attribute("DefaultLanguage")?.Value;
                            if (defLang != null && !langs.Contains(defLang))
                                langs.Add(defLang);
                        }

                        if (entryTemp.FullName.EndsWith("Hardware.xml"))
                        {
                            XElement xapp = catXML.Descendants(XName.Get("Product", ns)).ElementAt(0);
                            string defLang = xapp.Attribute("DefaultLanguage")?.Value;
                            if (defLang != null && !langs.Contains(defLang))
                                langs.Add(defLang);
                        }

                        if (entryTemp.FullName.EndsWith("Catalog.xml"))
                        {
                            XElement xapp = catXML.Descendants(XName.Get("CatalogSection", ns)).ElementAt(0);
                            string defLang = xapp.Attribute("DefaultLanguage")?.Value;
                            if (defLang != null && !langs.Contains(defLang))
                                langs.Add(defLang);
                        }

                        foreach (XElement lang in catXML.Descendants(XName.Get("Language", ns)))
                        {
                            string name = lang.Attribute("Identifier").Value;
                            if (!langs.Contains(name))
                                langs.Add(name);
                        }
                    }
                }
            }

            Archive.Dispose();
            Archive = null;

            return langs;
        }

        public override List<ImportDevice> GetDeviceList(CatalogContext _context = null)
        {
            if (Archive == null)
                Archive = ZipFile.OpenRead(_path);

            List<ImportDevice> devices = new List<ImportDevice>();
            List<string> manus = new List<string>();


            foreach (ZipArchiveEntry entryTemp in Archive.Entries)
            {
                if (entryTemp.FullName.StartsWith("M-"))
                {
                    string manName = "";
                    manName = entryTemp.FullName.Substring(0, 6);
                    if (!manus.Contains(manName))
                        manus.Add(manName);
                }
            }


            foreach (string manName in manus)
            {
                XElement catXML = XDocument.Load(Archive.GetEntry(manName + "/Catalog.xml").Open()).Root;
                XElement hardXML = XDocument.Load(Archive.GetEntry(manName + "/Hardware.xml").Open()).Root;
                TranslateXml(catXML, _language);
                TranslateXml(hardXML, _language);
                currentNamespace = catXML.Name.NamespaceName;

                IEnumerable<XElement> catalogItems = catXML.Descendants(GetXName("CatalogItem"));
                catalogItems = catalogItems.OrderBy(c => c.Attribute("Name").Value);
                Dictionary<string, string> images = new Dictionary<string, string>();

                if (File.Exists("Import/Images/KNX/" + manName + ".json"))
                {
                    images = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText("Import/Images/KNX/" + manName + ".json"));
                }

                foreach(XElement item in catalogItems)
                {
                    ImportDevice device = new ImportDevice()
                    {
                        Id = item.Attribute("Id").Value,
                        Name = item.Attribute("Name").Value,
                        Description = item.Attribute("VisibleDescription")?.Value.Replace(Environment.NewLine, " "),
                        Additional1 = item.Attribute("ProductRefId").Value,
                        Additional2 = item.Attribute("Hardware2ProgramRefId").Value,
                        ApplicationName = ""
                    };

                    if(_context != null)
                    {
                        //"M-00FA_H-00fa00002307-1_HP-0207-23-E298_CI-D7.2Dv23-1"
                        string[] items = device.Id.Split('-');
                        //TODO also search with manuId
                        Hardware2AppModel hard = _context.Hardware2App.SingleOrDefault(h => h.Number == items[2]);

                        if(hard != null)
                        {
                            int appNumber;
                            int appVersion;
                            if(int.TryParse(items[4], System.Globalization.NumberStyles.HexNumber, null, out appNumber) 
                            && int.TryParse(items[5], System.Globalization.NumberStyles.HexNumber, null, out appVersion))
                            {
                                device.ExistsInDatabase = _context.Applications.Any(a => a.HardwareId == hard.Id && a.Number == appNumber && a.Version == appVersion);
                            }
                        }
                    }


                    XElement prod = hardXML.Descendants().Single(d => d.Attribute("Id")?.Value == device.Additional1);
                    if(images.ContainsKey(device.Additional1))
                    {
                        device.ImageUrl = images[device.Additional1];
                    } else
                    {
                        device.ImageUrl = "https://th.bing.com/th/id/OIP.1OCQbyxf7bEvykoY1uiinwAAAA?pid=ImgDet&rs=1";
                    }
                    prod = hardXML.Descendants().Single(d => d.Attribute("Id")?.Value == device.Additional2);
                    foreach(XElement xapp in prod.Descendants(GetXName("ApplicationProgramRef")))
                    {
                        XElement xapp2 = XDocument.Load(Archive.GetEntry(manName + "/" + xapp.Attribute("RefId").Value + ".xml").Open()).Root;
                        //Todo get shorter translation of application
                        TranslateXml(xapp2, _language);
                        xapp2 = xapp2.Element(GetXName("ManufacturerData")).Element(GetXName("Manufacturer")).Element(GetXName("ApplicationPrograms")).Element(GetXName("ApplicationProgram"));
                        device.ApplicationName += " - " + xapp2.Attribute("Name").Value;
                    }
                    if (!string.IsNullOrEmpty(device.ApplicationName))
                        device.ApplicationName = device.ApplicationName.Substring(3);

                    devices.Add(device);
                }
            }

            Archive.Dispose();
            Archive = null;

            return devices;
        }

        public override void StartImport(List<ImportDevice> devices, CatalogContext context)
        {
            if (devices.Count == 0) return;
            ProgressCalculate(devices.Count);

            if (Archive == null)
                Archive = ZipFile.OpenRead(_path);

            _context = context;
            UpdateKnxMaster();

            System.Threading.Tasks.Task.Delay(2000);

            foreach(ImportDevice importDevice in devices) {
                CurrentDevice = importDevice.Name;
                CurrentState = "Importiere Gerät";


                
                int manuId = int.Parse(importDevice.Additional1.Substring(2, 4),  System.Globalization.NumberStyles.HexNumber);
                manuId = _context.Manufacturers.Single(m => m.ManuId == manuId && m.ImportType == ImportTypes.ETS).Id;
                DeviceViewModel device;
                bool deviceExists = _context.Devices.Any(d => d.ManufacturerId == manuId && d.Name == importDevice.Name);

                if(deviceExists)
                    device = _context.Devices.Single(d => d.ManufacturerId == manuId && d.Name == importDevice.Name);
                else {
                    device = new DeviceViewModel() {
                        Name = importDevice.Name,
                        VisibleDescription = importDevice.Description,
                        ManufacturerId = manuId
                    };
                }

                List<string> appIds = ImportHardware(importDevice, device);
                if(appIds.Count == 0)
                    continue;

                ImportCatalog(importDevice, device);


                if(deviceExists)
                    _context.Devices.Update(device);
                else
                    _context.Devices.Add(device);

                int count = 1;
                foreach(string appId in appIds) {
                    appName = $"Applikation {count++} von {appIds.Count}";
                    ImportApplication(importDevice, device, appId);
                }
            }

            _context.SaveChanges();

            Archive.Dispose();
            Archive = null;

            CurrentDevice = "Fertig";
            CurrentState = "Import abgeschlossen";
        }

        private void UpdateKnxMaster() {
            ZipArchiveEntry entry = Archive.GetEntry("knx_master.xml");
            if(entry == null)
            {
                Debug.WriteLine("Produktdatenbank enthält keine knx_master.xml");
                return;
            }
            
            XElement manXML = XDocument.Load(entry.Open()).Root;

            currentNamespace = manXML.Attribute("xmlns").Value;
            XElement mans = manXML.Element(GetXName("MasterData")).Element(GetXName("Manufacturers"));

            foreach (XElement manEle in mans.Elements())
            {
                int manuId = int.Parse(manEle.Attribute("Id").Value.Substring(2), System.Globalization.NumberStyles.HexNumber);
                if (!_context.Manufacturers.Any(m => m.ImportType == DataContext.Import.ImportTypes.ETS && m.ManuId == manuId))
                {
                    ManufacturerViewModel man = new ManufacturerViewModel
                    {
                        ManuId = manuId,
                        ImportType = DataContext.Import.ImportTypes.ETS,
                        Name = manEle.Attribute("Name").Value,
                    };
                    _context.Manufacturers.Add(man);
                }
            }
            _context.SaveChanges();
        }


#region "Importer"
        private void ImportApplication(ImportDevice device, DeviceViewModel model, string appId) {
            Bindings = new List<ParamBinding>();
            ComConditions = new Dictionary<int, List<List<ParamCondition>>>();

            string manu = appId.Substring(0, 6);
            ZipArchiveEntry entry = Archive.GetEntry($"{manu}/{appId}.xml");
            XElement xapp = XDocument.Load(entry.Open()).Root;
            CurrentState = appName + " - Übersetzen";
            TranslateXml(xapp, _language);
            xapp = xapp.Descendants(GetXName("ApplicationProgram")).ElementAt(0);
            CurrentState = appName + " - Infos";
            int appNum = GetAttributeAsInt(xapp, "ApplicationNumber");
            int appVer = GetAttributeAsInt(xapp, "ApplicationVersion");

            bool exists = _context.Applications.Any(a => a.Manufacturer == model.ManufacturerId && a.Number == appNum && a.Version == appVer);
            if(exists) return;

            ApplicationViewModel app = new ApplicationViewModel() {
                Name = GetAttributeAsString(xapp, "Name"),
                Number = appNum,
                Version = appVer,
                Mask = GetAttributeAsString(xapp, "MaskVersion"),
                Manufacturer = model.ManufacturerId,
                HardwareId = model.HardwareId
            };

            switch (xapp.Attribute("LoadProcedureStyle").Value)
            {
                case "ProductProcedure":
                    app.LoadProcedure = LoadProcedureTypes.Product;
                    break;

                case "MergedProcedure":
                    app.LoadProcedure = LoadProcedureTypes.Merge;
                    break;

                case "DefaultProcedure":
                    app.LoadProcedure = LoadProcedureTypes.Default;
                    break;

                default:
                    app.LoadProcedure = LoadProcedureTypes.Unknown;
                    break;
            }

            XElement xstatic = xapp.Element(GetXName("Static"));

            if(xstatic.Element(GetXName("ComObjectTable")) != null && xstatic.Element(GetXName("ComObjectTable")).Attribute("CodeSegment") != null) {
                XElement xtable = xstatic.Element(GetXName("ComObjectTable"));
                app.Table_Object = GetItemHexId(xtable.Attribute("CodeSegment").Value);
                if(xtable.Attribute("Offset") != null)
                    app.Table_Object_Offset = GetAttributeAsInt(xtable, "Offset");
            }
            if(xstatic.Element(GetXName("AddressTable")) != null) {
                XElement xtable = xstatic.Element(GetXName("AddressTable"));
                if(xtable.Attribute("CodeSegment") != null)
                    app.Table_Group = GetItemHexId(xtable.Attribute("CodeSegment").Value);
                if(xtable.Attribute("Offset") != null)
                    app.Table_Group_Offset = GetAttributeAsInt(xtable, "Offset");
                if(xtable.Attribute("MaxEntries") != null)
                    app.Table_Group_Max = GetAttributeAsInt(xtable, "MaxEntries");
            }
            if(xstatic.Element(GetXName("AssociationTable")) != null) {
                XElement xtable = xstatic.Element(GetXName("AssociationTable"));
                if(xtable.Attribute("CodeSegment") != null)
                    app.Table_Assosiations = GetItemHexId(xtable.Attribute("CodeSegment").Value);
                if(xtable.Attribute("Offset") != null)
                    app.Table_Assosiations_Offset = GetAttributeAsInt(xtable, "Offset");
                if(xtable.Attribute("MaxEntries") != null)
                    app.Table_Assosiations_Max = GetAttributeAsInt(xtable, "MaxEntries");
            }

            _context.Applications.Add(app);
            _context.SaveChanges();

            ImportBaggages(manu, xapp);

            ImportAppSegments(xapp, app.Id);
            ImportAppParaTypes(xapp, app.Id);

            counterUnion = 1;
            counterParameter = 1;
            counterComObjects = 0;
            ImportAppStatic(xstatic, app.Id);

            if(_context.AppParameters.Any(p => p.ApplicationId == app.Id))
            {
                int max = _context.AppParameters.Where(p => p.ApplicationId == app.Id).OrderByDescending(p => p.ParameterId).First().ParameterId;
                counterParameter = ++max;
            }

            if(_context.AppComObjects.Any(c => c.ApplicationId == app.Id))
            {
                int max = _context.AppComObjects.Where(c => c.ApplicationId == app.Id).OrderByDescending(c => c.Id).First().Id;
                counterComObjects = ++max;
            }

            if(xapp.Element(GetXName("ModuleDefs")) != null) {
                ImportModuleDefs(xapp, app.Id);
            }

            ImportDynamic(xapp.Element(GetXName("Dynamic")), app.Id);
        }

        private void ImportBaggages(string manu, XElement xapp)
        {
            XElement xextension = xapp.Element(GetXName("Static"))?.Element(GetXName("Extension"));
            if (xextension == null) return;

            List<string> baggs = new List<string>();

            foreach(XElement xbag in xextension.Descendants(GetXName("Baggage")))
            {
                baggs.Add(xbag.Attribute("RefId").Value);
            }

            ZipArchiveEntry entry = Archive.GetEntry($"{manu}/Baggages.xml");
            XElement xdoc = XElement.Load(entry.Open());

            foreach (XElement xbag in xdoc.Descendants(GetXName("Baggage")))
            {
                if (!baggs.Contains(xbag.Attribute("Id").Value)) continue;

                Baggage bag = new Baggage();
                bag.Id = xbag.Attribute("Id").Value;

                bag.TimeStamp = DateTime.Parse(xbag.Element(GetXName("FileInfo")).Attribute("TimeInfo").Value);
                string ext = bag.Id.ToLower().Substring(bag.Id.LastIndexOf('.') + 3);
                switch (ext)
                {
                    case "png":
                        bag.PictureType = PictureTypes.PNG;
                        break;

                    case "jpg":
                    case "jpeg":
                        bag.PictureType = PictureTypes.JPG;
                        break;

                    default:
                        throw new NotSupportedException("Dateiendung " + ext + " wird nicht unterstützt");
                }

                string path = xbag.Attribute("TargetPath").Value;
                string name = xbag.Attribute("Name").Value;
                string temp = Path.Combine(Path.GetTempPath(), "Kaenx-Importer");
                Directory.CreateDirectory(temp);

                if (!string.IsNullOrEmpty(path)) path = path + "/";

                ZipArchiveEntry file = Archive.GetEntry($"{manu}/Baggages/{path}{name}");
                file.ExtractToFile(Path.Combine(temp, name), true);

                byte[] data = File.ReadAllBytes(Path.Combine(temp, name));

                if(_context.Baggages.Any(b => b.Id == bag.Id))
                {
                    Baggage bago = _context.Baggages.Single(b => b.Id == bag.Id);
                    if (bag.TimeStamp < bago.TimeStamp) continue; //Skip older files
                    bago.Data = data;
                    _context.Baggages.Update(bago);
                } else
                {
                    bag.Data = data;
                    _context.Baggages.Add(bag);
                }
            }
        }

        private void ImportDynamic(XElement xdyn, int appId) {
            CurrentState = appName + " - Dynamische Ansicht";

            Dictionary<string, IDynChannel> Id2Channel = new Dictionary<string, IDynChannel>();
            Dictionary<string, ParameterBlock> Id2ParamBlock = new Dictionary<string, ParameterBlock>();
            List<IDynChannel> Channels = new List<IDynChannel>();

            foreach(XElement xele in xdyn.Descendants(GetXName("Channel")))
            {
                if(xele.Attribute("Text")?.Value == "")
                {
                    ChannelIndependentBlock cib2 = new ChannelIndependentBlock();
                    if (xele.Attribute("Access")?.Value == "None")
                    {
                        cib2.HasAccess = false;
                        cib2.IsVisible = false;
                    }
                    Channels.Add(cib2);
                    Id2Channel.Add(GetAttributeAsString(xele, "Id"), cib2);
                } else
                {
                    string text = "";
                    ChannelBlock cb = new ChannelBlock
                    {
                        Id = GetItemId(GetAttributeAsString(xele, "Id")),
                        Name = GetAttributeAsString(xele, "Name")
                    };
                    if (xele.Attribute("Access")?.Value == "None")
                    {
                        cb.HasAccess = false;
                        cb.IsVisible = false;
                    }

                    text = GetAttributeAsString(xele, "Text");

                    
                    cb.Text = CheckForBindings(cb, text, xele);
                    cb.Conditions = GetConditions(xele);
                    Channels.Add(cb);
                    Id2Channel.Add(GetAttributeAsString(xele, "Id"), cb);
                }
            }


            int blockCounter = 1;
            foreach(XElement xele in xdyn.Descendants(GetXName("ChannelIndependentBlock")))
            {
                ChannelIndependentBlock cib = new ChannelIndependentBlock();
                if (xele.Attribute("Access")?.Value == "None")
                {
                    cib.HasAccess = false;
                    cib.IsVisible = false;
                }
                Channels.Add(cib);
                int id = blockCounter++;
                xele.Add(new XAttribute("Id", "M-xx_A-xxxx-xx_CIB-" + id));
                Id2Channel.Add("M-xx_A-xxxx-xx_CIB-" + id, cib);
            }



            Assignments = new List<AssignParameter>();
            AppParas = new Dictionary<int, AppParameter>();
            AppParaTypes = new Dictionary<int, AppParameterTypeViewModel>();
            ComObjects = new Dictionary<int, AppComObject>();
            ComBindings = new List<ComBinding>();

            foreach (AppParameter para in _context.AppParameters.Where(p => p.ApplicationId == appId))
                AppParas.Add(para.ParameterId, para);

            foreach (AppParameterTypeViewModel type in _context.AppParameterTypes.Where(t => t.ApplicationId == appId))
                AppParaTypes.Add(type.Id, type);

            foreach (AppComObject co in _context.AppComObjects.Where(t => t.ApplicationId == appId))
                ComObjects.Add(co.Id, co);


            List<string> visibleConds = new List<string>();
            foreach(XElement xele in xdyn.Descendants(GetXName("choose")))
            {
                string id = xele.Attribute("ParamRefId").Value;
                if(!visibleConds.Contains(id))
                    visibleConds.Add(id);
            }
            

            foreach(XElement xele in xdyn.Descendants(GetXName("ParameterBlock")))
            {
                if(xele.Attribute("Inline")?.Value == "true") continue; //Tabellen überspringen
                string text = "";

                ParameterBlock pb = new ParameterBlock { Id = GetItemId(GetAttributeAsString(xele, "Id")) };
                if (GetAttributeAsString(xele, "Access") == "None")
                {
                    pb.HasAccess = false;
                    pb.IsVisible = false;
                }
                if (!string.IsNullOrEmpty(GetAttributeAsString(xele, "ParamRefId")))
                {
                    try
                    {
                        int paramId = GetItemId(GetAttributeAsString(xele, "ParamRefId"));
                        AppParameter para = _context.AppParameters.Single(p => p.ParameterId == paramId && p.ApplicationId == appId);
                        text = para.Text;
                        if (para.Access == AccessType.None)
                        {
                            pb.HasAccess = false;
                            pb.IsVisible = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        //Log.Error("Parameterblock TextRef Fehler!", ex);
                        throw new Exception("ParameterBlock TextRef Fehler", ex);
                    }
                }
                else
                    text = GetAttributeAsString(xele, "Text");

                pb.Text = CheckForBindings(pb, text, xele);
                pb.Conditions = GetConditions(xele);
                Id2ParamBlock.Add(GetAttributeAsString(xele, "Id"), pb);

                XElement xparent = xele.Parent;
                while(true) {
                    string name = xparent.Name.LocalName;
                    if(name == "Channel" || name == "ChannelIndependentBlock") {
                        IDynChannel ch = Id2Channel[GetAttributeAsString(xparent, "Id")];
                        ch.Blocks.Add(pb);
                        break;
                    } else if(name == "ParameterBlock") {
                        ParameterBlock pbp = Id2ParamBlock[GetAttributeAsString(xparent, "Id")];
                        pbp.Blocks.Add(pb);
                        break;
                    }
                    xparent = xparent.Parent;
                }

                int textRefId = -2;
                if (xele.Attribute("TextParameterRefId") != null)
                {
                    textRefId = GetItemId(xele.Attribute("TextParameterRefId").Value);
                } else {
                    XElement temp = xele.Parent;
                    while (textRefId != -2 || temp.Name.LocalName == "Dynamic")
                    {
                        temp = temp.Parent;
                        textRefId = GetItemId(temp.Attribute("TextParameterRefId")?.Value);
                    }
                }

                GetChildItems(pb, xele, visibleConds);
            }


            Dictionary<long, IValues> values = new Dictionary<long, IValues>();
            foreach(AppParameter para in AppParas.Values)
            {
                values[para.ParameterId] = new StandardValues(para.Value);
            }
            //TODO Assignments beachten

            List<int> defaultComs = new List<int>();
            foreach(AppComObject com in ComObjects.Values)
            {
                if (defaultComs.Contains(com.UId)) continue;

                if (!ComBindings.Any(c => c.ComId == com.Id))
                    defaultComs.Add(com.UId);
                else
                {
                    foreach(ComBinding bind in ComBindings.Where(c => c.ComId == com.Id))
                    {
                        if(FunctionHelper.CheckConditions(bind.Conditions, values))
                        {
                            defaultComs.Add(com.UId);
                            break;
                        }
                    }
                }
            }

            foreach (IDynChannel ch in Channels)
            {
                if(ch.HasAccess)
                    ch.IsVisible = FunctionHelper.CheckConditions(ch.Conditions, values);
                foreach (ParameterBlock pb in ch.Blocks)
                {
                    if (pb.HasAccess)
                        pb.IsVisible = FunctionHelper.CheckConditions(pb.Conditions, values);

                    foreach (IDynParameter para in pb.Parameters)
                    {
                        if (para.HasAccess)
                            para.IsVisible = FunctionHelper.CheckConditions(para.Conditions, values);
                    }
                }
            }

            AppAdditional adds = new AppAdditional() {
                ApplicationId = appId
            };
            adds.Bindings = FunctionHelper.ObjectToByteArray(Bindings, true);
            adds.ParamsHelper = FunctionHelper.ObjectToByteArray(Channels, true, "Kaenx.DataContext.Import.Dynamic");
            adds.Assignments = FunctionHelper.ObjectToByteArray(Assignments, true);
            adds.ComsAll = FunctionHelper.ObjectToByteArray(ComBindings, true);
            adds.ComsDefault = System.Text.Encoding.UTF8.GetBytes(string.Join(",", defaultComs));

            XElement xload = xdyn.Parent.Element(GetXName("Static")).Element(GetXName("LoadProcedures"));
            if (xload != null)
                adds.LoadProcedures = Encoding.UTF8.GetBytes(xload.ToString());

            _context.AppAdditionals.Add(adds);
            _context.SaveChanges();
        }

        public string CheckForBindings(ChannelBlock channel, string text, XElement xele, Dictionary<string, string> args = null, Dictionary<string, int> idMapper = null) {
            return CheckForBindings(text, BindingTypes.Channel, channel.Id, xele, args, idMapper);
        }

        public string CheckForBindings(ParameterBlock block, string text, XElement xele, Dictionary<string, string> args = null, Dictionary<string, int> idMapper = null) {
            return CheckForBindings(text, BindingTypes.ParameterBlock, block.Id,  xele, args, idMapper);
        }

        public string CheckForBindings(AppComObject com, string text, XElement xele, Dictionary<string, string> args = null, Dictionary<string, int> idMapper = null){
            return CheckForBindings(text, BindingTypes.ComObject, com.Id, xele, args, idMapper);
        }

        public string CheckForBindings(string text, BindingTypes type, int targetId, XElement xele, Dictionary<string, string> args, Dictionary<string, int> idMapper) {
            Regex reg = new Regex("{{(.*)}}"); //[A-Za-z0-9: -]
            if(reg.IsMatch(text)){
                Match match = reg.Match(text);
                string g2 = match.Groups[1].Value;
                if(args != null && args.ContainsKey(g2)) {
                    //Argument von Modul einsetzen
                    return text.Replace(match.Groups[0].Value, args[g2]);
                }

                ParamBinding bind = new ParamBinding()
                {
                    Type = type,
                    TargetId = targetId,
                    FullText = text.Replace(match.Groups[0].Value, "{d}")
                };
                //Text beinhaltet ein Binding zu einem Parameter
                try{
                if(g2.Contains(':')){
                    string[] opts = g2.Split(':');
                    text = text.Replace(match.Groups[0].Value, opts[1]);
                    bind.SourceId = opts[0] == "0" ? -1 : int.Parse(opts[0]);
                    bind.DefaultText = opts[1];
                } else {
                    text = text.Replace(match.Groups[0].Value, "");
                    bind.SourceId = g2 == "0" ? -1 : int.Parse(g2);
                    bind.DefaultText = "";
                }
                }catch{

                }
                
                if(bind.SourceId == -1) {
                    if(xele.Attribute("TextParameterRefId") != null)
                    {
                        string bindId = GetAttributeAsString(xele, "TextParameterRefId");
                        if (string.IsNullOrEmpty(bindId))
                            throw new Exception("Kein TextParameterRefId für KO gefunden");
                        bind.SourceId = GetItemId(bindId);
                    } else
                    {
                        throw new Exception("Object enthält dynamischen Text mit Referenz 0, hat aber kein Attribut mit TextParameterRefId");
                        //XElement xstatic = xele;
                        //while (true)
                        //{
                        //    xstatic = xstatic.Parent;
                        //    if (xstatic.Name.LocalName == "Static") break;
                        //}
                        //if (xstatic.Parent.Element(GetXName("Dynamic")) != null)
                        //{
                        //    XElement xdyn = xstatic.Parent.Element(GetXName("Dynamic"));
                        //    XElement xref = xdyn.Descendants(GetXName("ComObjectRefRef")).First(co => co.Attribute("RefId").Value == GetAttributeAsString(xele, "Id"));

                        //    while (true)
                        //    {
                        //        xref = xref.Parent;
                        //        if (xref.Name.LocalName == "ParameterBlock") break;
                        //    }
                        //    string bindId = GetAttributeAsString(xref, "TextParameterRefId");
                        //    if (string.IsNullOrEmpty(bindId))
                        //        throw new Exception("Kein TextParameterRefId für KO gefunden");
                        //    bind.SourceId = GetItemId(bindId);
                        //}
                    }
                }
                
                Bindings.Add(bind);
            }
            return text;
        }



        public void GetChildItems(ParameterBlock block, XElement xparent, List<string> visibleConds) {
            foreach(XElement xele in xparent.Elements())
            {
                switch (xele.Name.LocalName)
                {
                    case "when":
                    case "choose":
                        GetChildItems(block, xele, visibleConds);
                        break;
                    case "ParameterRefRef":
                        ParseParameterRefRef(xele, block, visibleConds);
                        break;
                    case "ParameterSeparator":
                        ParseSeparator(xele, block);
                        break;
                    case "ComObjectRefRef":
                        ParseComObject(xele);
                        break;
                    case "Assign":
                        AssignParameter assign = new AssignParameter
                        {
                            Target = GetItemId(xele.Attribute("TargetParamRefRef").Value),
                            Conditions = GetConditions(xele)
                        };
                        if (xele.Attribute("SourceParamRefRef") != null)
                        {
                            assign.Source = GetItemId(xele.Attribute("SourceParamRefRef").Value);
                        }
                        else
                        {
                            assign.Source = -1;
                            assign.Value = xele.Attribute("Value").Value;
                        }
                        Assignments.Add(assign);
                        break;

                    case "ParameterBlock":
                        if(xele.Attribute("Inline")?.Value != "true") continue; //Nur Tabellen bearbeiten
                        ParseTable(block, xele, visibleConds);
                        break;
                }
            }
        }

        private void ImportModuleDefs(XElement xapp, int appId) {
            Dictionary<string, KnxProdAllocators> allocs = new Dictionary<string, KnxProdAllocators>();

            if(xapp.Element(GetXName("Static")).Element(GetXName("Allocators")) != null){
                foreach(XElement xalloc in xapp.Element(GetXName("Static")).Element(GetXName("Allocators")).Elements()){
                    KnxProdAllocators alloc = new KnxProdAllocators() {
                        Index = int.Parse(xalloc.Attribute("Start").Value),
                        Increase = int.Parse(xalloc.Attribute("maxInclusive").Value)
                    };
                    allocs.Add(xalloc.Attribute("Id").Value, alloc);
                }
            }

            XElement xdyn = xapp.Element(GetXName("Dynamic"));
            foreach(XElement xmod in xdyn.Descendants(GetXName("Module"))){
                Dictionary<string, string> args = new Dictionary<string, string>();
                foreach(XElement xarg in xmod.Elements()){
                    if(xarg.Attribute("AllocatorRefId") != null){
                        KnxProdAllocators alloc = allocs[xarg.Attribute("AllocatorRefId").Value];
                        if(alloc.Started) {
                            alloc.Index = alloc.Increase;
                        }
                        args.Add(xarg.Attribute("RefId").Value, alloc.Index.ToString());
                    } else {
                        args.Add(xarg.Attribute("RefId").Value, xarg.Attribute("Value")?.Value);
                    }
                }
                    

                XElement xdef = xapp.Descendants(GetXName("ModuleDef")).Single(md => md.Attribute("Id").Value == xmod.Attribute("RefId").Value);

                foreach(XElement xarg in xdef.Element(GetXName("Arguments")).Elements()){
                    if(xarg.Attribute("Name") != null) {
                        string val = args[xarg.Attribute("Id").Value];
                        args.Add(xarg.Attribute("Name").Value, val);
                    }
                }
            
                Dictionary<string, int> idMapper = ImportAppStatic(xdef.Element(GetXName("Static")), appId, args);

                XElement xmoddyn = XElement.Parse(xdef.Element(GetXName("Dynamic")).ToString());
                RenameDynamic(xmoddyn, args, idMapper);
                
                foreach(XElement xele in xmoddyn.Elements()){
                    xmod.AddAfterSelf(xele);
                }
            }

            List<XElement> xmodules = xdyn.Descendants(GetXName("Module")).ToList();
            foreach(XElement xmod in xmodules){
                xmod.Remove();
            }

            //Give each ParameterBlock an unique ID
            int blockCounter = 1;
            foreach(XElement xblock in xdyn.Descendants(GetXName("ParameterBlock"))) {
                string id = GetAttributeAsString(xblock, "Id");
                id = id.Substring(0, id.LastIndexOf("-")+1);
                id += blockCounter++;
                xblock.Attribute("Id").Value = id;
            }
        }

        public void RenameDynamic(XElement xdyn, Dictionary<string, string> args, Dictionary<string, int> idMapper) {
            List<XElement> xobjs = new List<XElement>();
            xobjs.AddRange(xdyn.Descendants(GetXName("ParameterBlock")));
            xobjs.AddRange(xdyn.Descendants(GetXName("ParameterRefRef")));
            xobjs.AddRange(xdyn.Descendants(GetXName("choose")));

            foreach(XElement xref in xobjs){
                int id = 0;

                switch(xref.Name.LocalName)
                {
                    case "ParameterBlock":
                    {
                        if(xref.Attribute("TextParameterRefId") != null)
                        {
                            id = GetItemId(xref.Attribute("TextParameterRefId").Value);
                            int newId = idMapper["p"+id];
                            xref.Attribute("TextParameterRefId").Value = "xx_R-" + newId;
                        }
                        break;
                    }

                    case "ParameterRefRef":
                    {
                        id = GetItemId(xref.Attribute("RefId").Value);
                        int newId = idMapper["p"+id];
                        xref.Attribute("RefId").Value = "xx_R-" + newId;
                        break;
                    }

                    case "choose":
                    {
                        id = GetItemId(xref.Attribute("ParamRefId").Value);
                        int newId = idMapper["p"+id];
                        xref.Attribute("ParamRefId").Value = "xx_R-" + newId;
                        break;
                    }

                    default:
                        throw new Exception("Not implemented");
                }
            }

            foreach(XElement xref in xdyn.Descendants(GetXName("ComObjectRefRef"))){
                int id = GetItemId(xref.Attribute("RefId").Value);
                int newId = idMapper["c"+id];
                xref.Attribute("RefId").Value = "xx_R-" + newId;
            }

            xobjs.Clear();
            xobjs.AddRange(xdyn.Descendants(GetXName("ChannelIndependentBlock")));
            xobjs.AddRange(xdyn.Descendants(GetXName("Channel")));
            xobjs.AddRange(xdyn.Descendants(GetXName("ParameterBlock")));
            
            Regex reg = new Regex("{{([A-Za-z0-9:]*)}}");

            foreach(XElement xobj in xobjs){
                string temp = xobj.Attribute("Text").Value;
                if(reg.IsMatch(temp)) {
                    Match match = reg.Match(temp);
                    string g2 = match.Groups[1].Value;
                    if(args.ContainsKey(g2)){
                        xobj.Attribute("Text").Value = temp.Replace("{{" + g2 + "}}", args[g2]);
                    }
                }

                if(xobj.Attribute("Number") != null) {
                    string numb = xobj.Attribute("Number").Value;
                    if(args.ContainsKey(numb))
                    {
                        xobj.Attribute("Number").Value = args[numb];
                        string id = xobj.Attribute("Id").Value;
                        id = id.Replace(numb, args[numb]);
                        xobj.Attribute("Id").Value = id;
                    }
                }

                if(xobj.Attribute("ParamRefId") != null) {
                    int id = GetItemId(xobj.Attribute("ParamRefId").Value);
                    int newId = idMapper["p"+id];
                    xobj.Attribute("ParamRefId").Value = "xx_R-" + newId;
                }
            }
        }

        private Dictionary<string, int> ImportAppStatic(XElement xstatic, int appId, Dictionary<string, string> args = null) {
            if(args == null) CurrentState = appName + " - Parameter";
            Dictionary<string, int> idMapper = new Dictionary<string, int>(); //Mapping old Ids to new one if a ModuleDef is beeing used


            if(xstatic.Element(GetXName("Parameters")) != null) {
                Dictionary<int, AppParameter> parameters = new Dictionary<int, AppParameter>();
                foreach(XElement xitem in xstatic.Element(GetXName("Parameters")).Elements()) {
                    if(xitem.Name.LocalName == "Union") {
                        XElement xmem = xitem.Element(GetXName("Memory"));
                        foreach(XElement xpara in xitem.Elements(GetXName("Parameter")))
                            ParseParameter(parameters, args, xpara, xmem, counterUnion);
                        counterUnion++;
                    } else if(xitem.Name.LocalName == "Parameter") {
                        XElement xmem = null;
                        if(xitem.Elements().Count() > 0) xmem = xitem.Elements().ElementAt(0);
                        ParseParameter(parameters, args, xitem, xmem);
                    } else {
                        //TODO log
                        throw new Exception("Kein bekannter Typ bei Parameters: " + xitem.Name.LocalName);
                    }
                }

                ParseParameterRefs(parameters, xstatic.Element(GetXName("ParameterRefs")), appId, args != null ? idMapper : null);
            }





            if(args == null) CurrentState = appName + " - Kommunikationsobjekte";
            if(xstatic.Element(GetXName("ComObjectRefs")) != null) {
                ParseComObjects(xstatic, appId, args, args != null ? idMapper : null);
            }


            _context.SaveChanges();
            return idMapper;
        }

        private void ImportAppSegments(XElement xapp, int appId) {
            CurrentState = appName + " - Segments";
            IEnumerable<XElement> xsegments = xapp.Element(GetXName("Static")).Element(GetXName("Code")).Elements();

            foreach (XElement seg in xsegments)
            {
                switch (seg.Name.LocalName)
                {
                    case "AbsoluteSegment":
                        //TODO check if neccessary
                        //app.IsRelativeSegment = false;
                        AppSegmentViewModel aas = new AppSegmentViewModel();

                        aas.ApplicationId = appId;
                        aas.SegmentId = GetItemHexId(GetAttributeAsString(seg, "Id"));
                        aas.Address = GetAttributeAsInt(seg, "Address");
                        aas.Size = GetAttributeAsInt(seg, "Size");
                        aas.Data = seg.Element(GetXName("Data"))?.Value;
                        aas.Mask = seg.Element(GetXName("Mask"))?.Value;
                        _context.AppSegments.Add(aas);
                        break;

                    case "RelativeSegment":
                        //app.IsRelativeSegment = true;
                        AppSegmentViewModel ars = new AppSegmentViewModel();

                        ars.ApplicationId = appId;
                        ars.SegmentId = GetItemHexId(GetAttributeAsString(seg, "Id"));
                        ars.Offset = GetAttributeAsInt(seg, "Offset");
                        ars.Size = GetAttributeAsInt(seg, "Size");
                        ars.LsmId = GetAttributeAsInt(seg, "LoadStateMachine");
                        ars.Data = seg.Element(GetXName("Data"))?.Value;
                        ars.Mask = seg.Element(GetXName("Mask"))?.Value;
                        _context.AppSegments.Add(ars);
                        break;

                    default:
                        throw new NotImplementedException("Unbekanntes Segment: " + seg.Name.LocalName);
                        //msg = "Unbekanntes Segment: " + seg.Name.LocalName;
                        //if (!Errors.Contains(msg))
                        //    Errors.Add(msg);
                        //TODO Log
                }
            }
            _context.SaveChanges();
        }

        private void ImportAppParaTypes(XElement xapp, int appId) {
            CurrentState = appName + " - ParamTypes";
            parameterTypeIds = new Dictionary<string, int>();
            IEnumerable<XElement> xparaTypes = xapp.Descendants(GetXName("ParameterType"));

            foreach(XElement xparaType in xparaTypes) {
                AppParameterTypeViewModel model = new AppParameterTypeViewModel() {
                    //Name = GetAttributeAsString(xparaType, "Name"),
                    ApplicationId = appId
                };
                model.Name = GetAttributeAsString(xparaType, "Id").Substring(GetAttributeAsString(xparaType, "Id").LastIndexOf("-") + 1);

                bool modelAdded = false;
                XElement child = xparaType.Elements().ElementAt(0);

                switch(child.Name.LocalName) {
                    case "TypeNumber":
                        if (child.Attribute("UIHint")?.Value == "CheckBox")
                        {
                            model.Type = ParamTypes.CheckBox;
                        } else if(child.Attribute("UIHint")?.Value == "Slider")
                        {
                            model.Type = ParamTypes.Slider;
                        } else 
                        { 
                            switch (child.Attribute("Type").Value)
                            {
                                case "signedInt":
                                    model.Type = ParamTypes.NumberInt;
                                    break;
                                case "unsignedInt":
                                    model.Type = ParamTypes.NumberUInt;
                                    break;
                                default:
                                    //msg = "Unbekannter Nummerntype: " + child.Attribute("Type").Value;
                                    //if (!Errors.Contains(msg))
                                    //    Errors.Add(msg);
                                    //Log.Error("Unbekannter Nummerntyp: " + child.Name.LocalName + " - " + child.Attribute("Type").Value);
                                    //TODO Log
                                    throw new NotImplementedException("Unbekannter TypeNumber: " + child.Attribute("Type").Value);
                            }
                        }
                        model.Size = int.Parse(child.Attribute("SizeInBit").Value);
                        model.Tag1 = child.Attribute("minInclusive").Value;
                        model.Tag2 = child.Attribute("maxInclusive").Value;
                        break;
                    case "TypeTime":
                        model.Type = ParamTypes.Time;
                        model.Size = int.Parse(child.Attribute("SizeInBit").Value);
                        model.Tag1 = child.Attribute("minInclusive").Value + ";" + child.Attribute("Unit").Value;
                        model.Tag2 = child.Attribute("maxInclusive").Value;
                        break;

                    case "TypeRestriction":
                        model.Type = ParamTypes.Enum;
                        model.Size = int.Parse(child.Attribute("SizeInBit").Value);
                        _context.AppParameterTypes.Add(model);
                        _context.SaveChanges(); // Damit die paramt schon eine ID haben
                        modelAdded = true;
                        string _base = child.Attribute("Base").Value;
                        int cenu = 0;
                        foreach (XElement en in child.Elements().ToList())
                        {
                            AppParameterTypeEnumViewModel enu = new AppParameterTypeEnumViewModel
                            {
                                TypeId = model.Id, //TODO this is not anymore. just count up, Tag1 ist number which also counts up
                                ParameterId = GetItemId(en.Attribute("Id").Value)
                            };
                            
                            //TODO prüfen ob langdauernde abfrage notwendig ist
                            enu.Value = en.Attribute(_base).Value;
                            enu.Text = en.Attribute("Text").Value;
                            enu.Order = (en.Attribute("DisplayOrder") == null) ? cenu : int.Parse(en.Attribute("DisplayOrder").Value);
                            cenu++;
                            
                            _context.AppParameterTypeEnums.Add(enu);
                        }
                        _context.SaveChanges(); // Damit die paramt schon eine ID haben
                        break;
                    case "TypeText":
                        model.Type = ParamTypes.Text;
                        model.Size = int.Parse(child.Attribute("SizeInBit").Value);
                        break;
                    case "TypeFloat":
                        switch (child.Attribute("Encoding").Value)
                        {
                            case "DPT 9":
                                model.Type = ParamTypes.Float9;
                                break;
                            default:
                                break;
                        }
                        model.Tag1 = child.Attribute("minInclusive").Value;
                        model.Tag2 = child.Attribute("maxInclusive").Value;
                        break;
                    case "TypePicture":
                        model.Type = ParamTypes.Picture;
                        model.Tag1 = GetItemId(child.Attribute("RefId").Value).ToString();
                        break;
                    case "TypeIPAddress":
                        model.Type = ParamTypes.IpAdress;
                        model.Tag1 = child.Attribute("AddressType").Value;
                        model.Size = 4 * 8;
                        break;
                    case "TypeNone":
                        model.Type = ParamTypes.None;
                        break;
                    case "TypeColor":
                        model.Type = ParamTypes.Color;
                        model.Tag1 = child.Attribute("Space").Value;
                        break;

                    default:
                        //msg = "Unbekannter Parametertype: " + child.Name.LocalName;
                        //if (!Errors.Contains(msg))
                        //    Errors.Add(msg);
                        //Log.Error("Unbekannter Parametertyp: " + child.Name.LocalName);
                        //TODO Log
                        throw new NotImplementedException("Unbekannter ParameterType: " + child.Name.LocalName);
                }
                if(!modelAdded) _context.AppParameterTypes.Add(model);
            }
            _context.SaveChanges();


            foreach(AppParameterTypeViewModel model in _context.AppParameterTypes.Where(pt => pt.ApplicationId == appId)) {
                parameterTypeIds.Add(model.Name, model.Id); //just count up here, dont save again
            }
        }

        private void ImportCatalog(ImportDevice device, DeviceViewModel model) {
            foreach(ZipArchiveEntry entry in Archive.Entries)
            {
                if (entry.Name != "Catalog.xml") continue;

                string manu = entry.FullName.Substring(2, 4);
                int manuId = int.Parse(manu, System.Globalization.NumberStyles.HexNumber);

                string manuName = _context.Manufacturers.Single(m => m.ManuId == manuId && m.ImportType == ImportTypes.ETS).Name;

                CatalogViewModel parent = null;

                if (!_context.Sections.Any(s => s.ImportType == DataContext.Import.ImportTypes.ETS && s.Name == manuName))
                {
                    ManufacturerViewModel man = _context.Manufacturers.Single(m => m.ImportType == DataContext.Import.ImportTypes.ETS && m.ManuId == manuId);
                    parent = new CatalogViewModel
                    {
                        ImportType = DataContext.Import.ImportTypes.ETS,
                        Name = man.Name,
                        ParentId = -1
                    };
                    _context.Sections.Add(parent);
                    _context.SaveChanges();
                } else
                {
                    parent = _context.Sections.Single(s => s.ImportType == DataContext.Import.ImportTypes.ETS && s.Name == manuName);
                }

                XElement catalog = XDocument.Load(entry.Open()).Root;
                TranslateXml(catalog, _language);
                currentNamespace = catalog.Name.NamespaceName;
                catalog = catalog.Element(GetXName("ManufacturerData")).Element(GetXName("Manufacturer")).Element(GetXName("Catalog"));


                XElement catalogitem = catalog.Descendants().Single(ci => ci.Attribute("Id").Value == device.Id);
                List<XElement> toadd = new List<XElement>();

                while(catalogitem.Parent.Name.LocalName != "Catalog"){
                    catalogitem = catalogitem.Parent;
                    toadd.Add(catalogitem);
                }

                int parentId = parent.Id;

                for(int i = toadd.Count -1; i >= 0; i--) {
                    CatalogViewModel section = null;
                    string name = GetAttributeAsString(toadd[i], "Name");
                    if (!_context.Sections.Any(s => s.Name == name && s.ParentId == parentId))
                    {
                        section = new CatalogViewModel
                        {
                            ImportType = ImportTypes.ETS,
                            Name = name,
                            ParentId = parentId
                        };
                        _context.Sections.Add(section);
                        _context.SaveChanges();
                        parentId = section.Id;
                    }
                    else
                    {
                        section = _context.Sections.Single(s => s.Name == name && s.ParentId == parentId);
                        parentId = section.Id;
                    }
                }
               
                model.CatalogId = parentId;
            }

            _context.SaveChanges();
        }

        private List<string> ImportHardware(ImportDevice device, DeviceViewModel model) {
            List<string> appIds = new List<string>();

            string manu = device.Additional1.Substring(0, 6);
            XElement xml;
            try
            {
                ZipArchiveEntry entry = Archive.GetEntry(manu + "/Hardware.xml");
                xml = XDocument.Load(entry.Open()).Root;
                TranslateXml(xml, _language);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Hardware Fehler!" + ex.Message);
                //Log.Error(e, "Hardware Fehler!");
                //OnError?.Invoke(device.Name + ": " + e.Message + Environment.NewLine + e.StackTrace);
                return appIds;
            }

            int manuId = int.Parse(manu.Substring(2),  System.Globalization.NumberStyles.HexNumber);
            manuId = _context.Manufacturers.Single(m => m.ManuId == manuId && m.ImportType == ImportTypes.ETS).Id;

            currentNamespace = xml.Name.NamespaceName;
            XElement productXml = xml.Descendants(GetXName("Product")).Single(p => p.Attribute("Id").Value == device.Additional1);
            XElement hardwareXml = productXml.Parent.Parent;
            XElement hardware2ProgXml = xml.Descendants(GetXName("Hardware2Program")).Single(p => p.Attribute("Id").Value == device.Additional2);

            model.ManufacturerId = manuId;
            model.OrderNumber = productXml.Attribute("OrderNumber").Value;
            model.BusCurrent = (int)GetAttributeAsFloat(hardwareXml, "BusCurrent");
            model.IsRailMounted = GetAttributeAsBool(productXml, "IsRailMounted");
            model.IsPowerSupply = GetAttributeAsBool(hardwareXml, "IsPowerSupply");
            model.IsCoupler = GetAttributeAsBool(hardwareXml, "IsCoupler");
            model.HasApplicationProgram = GetAttributeAsBool(hardwareXml, "HasApplicationProgram");
            model.HasIndividualAddress = GetAttributeAsBool(hardwareXml, "HasIndividualAddress");

            if (model.BusCurrent == 0 && model.HasApplicationProgram)
                model.BusCurrent = 10;
            
            string snum = GetAttributeAsString(hardwareXml, "SerialNumber");
            int vnum = GetAttributeAsInt(hardwareXml, "VersionNumber");
            Hardware2AppModel hard = null;
            bool hard2appExists = _context.Hardware2App.Any(h => h.ManuId == manuId && h.Number == snum && h.Version == vnum);
            if (hard2appExists)
            {
                hard = _context.Hardware2App.Single(h => h.ManuId == manuId && h.Number == snum && h.Version == vnum);
                model.HardwareId = hard.Id;
            } else
            {
                hard = new Hardware2AppModel()
                {
                    ManuId = manuId,
                    Number = snum,
                    Version = vnum
                };
                _context.Hardware2App.Add(hard);
                _context.SaveChanges();
                model.HardwareId = hard.Id;
            }

            foreach(XElement prog in hardware2ProgXml.Elements(GetXName("ApplicationProgramRef"))) {
                appIds.Add(GetAttributeAsString(prog, "RefId"));
            }

            

            return appIds;
        }
#endregion



#region "Parser"

        private void ParseTable(ParameterBlock block, XElement xele, List<string> visibleConds) {
            ParameterBlock fakeBlock = new ParameterBlock();
            GetChildItems(fakeBlock, xele, visibleConds);
            ParameterTable table = new ParameterTable() {
                Id = GetItemId(xele.Attribute("Id").Value),
                Conditions = GetConditions(xele)
            };
            table.Parameters = fakeBlock.Parameters;
            
            foreach(XElement xrow in xele.Element(XName.Get("Rows", xele.Name.NamespaceName)).Elements()) {
                string height = xrow.Attribute("Height")?.Value;
                TableRow row = new TableRow();
                if(!string.IsNullOrEmpty(height)) {
                    if(height.Contains("%")){
                        row.Unit = UnitTypes.Percentage;
                        row.Height = int.Parse(height.Replace("%", ""));
                    }
                }
                table.Rows.Add(row);
            }

            foreach(XElement xcol in xele.Element(XName.Get("Columns", xele.Name.NamespaceName)).Elements()) {
                string width = xcol.Attribute("Width")?.Value;
                TableColumn col = new TableColumn();
                if(!string.IsNullOrEmpty(width)) {
                    if(width.Contains("%")){
                        col.Unit = UnitTypes.Percentage;
                        col.Width = int.Parse(width.Replace("%", ""));
                    }
                }
                table.Columns.Add(col);
            }

            foreach(XElement position in xele.Elements()){
                if(position.Name.LocalName == "Rows" || position.Name.LocalName == "Columns") continue;

                TablePosition pos = new TablePosition();
                string[] posStr = position.Attribute("Cell").Value.Split(',');
                pos.Row = int.Parse(posStr[0]);
                pos.Column = int.Parse(posStr[1]);
                table.Positions.Add(pos);
            }


            block.Parameters.Add(table);
        }

        private void ParseComObject(XElement xele)
        {
            List<ParamCondition> conds = GetConditions(xele);

            if(conds.Count > 0)
            {
                ComBindings.Add(new ComBinding()
                {
                    ComId = GetItemId(xele.Attribute("RefId").Value),
                    Conditions = conds
                });
            }
        }

        private void ParseSeparator(XElement xele, ParameterBlock block)
        {
            int vers = int.Parse(xele.Name.NamespaceName.Substring(xele.Name.NamespaceName.LastIndexOf("/") + 1));

            List<ParamCondition> Conds = GetConditions(xele, true);

            string hint = xele.Attribute("UIHint")?.Value;

            IDynParameter sep;
            switch (hint)
            {
                case null:
                case "Headline":
                    sep = new ParamSeperator();
                    if (hint == "Headline")
                        (sep as ParamSeperator).Hint = ParamSeparatorHint.Headline;
                    else
                        (sep as ParamSeperator).Hint = ParamSeparatorHint.None;
                    break;

                case "HorizontalRuler":
                    sep = new ParamSeperator() { Hint = ParamSeparatorHint.HorizontalRuler };
                    break;

                case "Error":
                    sep = new ParamSeperatorBox() { Hint = ParamSeparatorHint.Error };
                    break;

                case "Information":
                    sep = new ParamSeperatorBox() { Hint = ParamSeparatorHint.Information };
                    break;

                default:
                    throw new NotImplementedException("Unbekannter UIHint: " + hint);
            }

            sep.Conditions = Conds;
            sep.Id = GetItemId(xele.Attribute("Id").Value);
            sep.Text = xele.Attribute("Text").Value;
            block.Parameters.Add(sep);
        }

        private void ParseParameterRefRef(XElement xele, ParameterBlock block, List<string> visibleConds)
        {
            AppParameter para = AppParas[GetItemId(xele.Attribute("RefId").Value)];
            AppParameterTypeViewModel paraType = AppParaTypes[para.ParameterTypeId];
            var paramList = GetConditions(xele, true);

            int refid = para.Id;

            bool hasAccess = para.Access != AccessType.None;
            bool IsCtlEnabled = para.Access != AccessType.Read;
            bool isVisibleCond = visibleConds.Contains(xele.Attribute("RefId").Value);

            switch (paraType.Type)
            {
                case ParamTypes.None:
                    IDynParameter paran = new ParamNone
                    {
                        Id = para.ParameterId,
                        Text = para.Text,
                        SuffixText = para.SuffixText,
                        Default = para.Value,
                        Value = para.Value,
                        Conditions = paramList,
                        HasAccess = hasAccess,
                        IsEnabled = IsCtlEnabled,
                        IsVisibleCondition = isVisibleCond
                    };
                    block.Parameters.Add(paran);
                    break;

                case ParamTypes.IpAdress:
                    IDynParameter pip;
                    if (para.Access == AccessType.Read)
                        pip = new ParamTextRead();
                    else
                        pip = new ParamText() { IsVisibleCondition = isVisibleCond };
                    pip.Id = para.ParameterId;
                    pip.Text = para.Text;
                    pip.SuffixText = para.SuffixText;
                    pip.Default = para.Value;
                    pip.Value = para.Value;
                    pip.Conditions = paramList;
                    pip.HasAccess = hasAccess;
                    pip.IsEnabled = IsCtlEnabled;
                    block.Parameters.Add(pip);
                    break;

                case ParamTypes.Slider:
                    Dynamic.ParamSlider ps = new ParamSlider
                    {
                        Id = para.ParameterId,
                        Text = para.Text,
                        SuffixText = para.SuffixText,
                        Value = para.Value,
                        Default = para.Value,
                        Conditions = paramList,
                        HasAccess = hasAccess,
                        IsEnabled = IsCtlEnabled,
                        IsVisibleCondition = isVisibleCond
                    };
                    try{
                        ps.Minimum = double.Parse(paraType.Tag1);
                        ps.Maximum = double.Parse(paraType.Tag2);
                    } catch{
                        Debug.WriteLine("Can't convert Min/Max to Double: " + paraType.Tag1 + "/" + paraType.Tag2);
                    }
                    //TODO add min/max/inc
                    block.Parameters.Add(ps);
                    break;

                case ParamTypes.NumberInt:
                case ParamTypes.NumberUInt:
                case ParamTypes.Float9:
                    Dynamic.ParamNumber pnu = new Dynamic.ParamNumber
                    {
                        Id = para.ParameterId,
                        Text = para.Text,
                        SuffixText = para.SuffixText,
                        Value = para.Value,
                        Default = para.Value,
                        Conditions = paramList,
                        HasAccess = hasAccess,
                        IsEnabled = IsCtlEnabled,
                        IsVisibleCondition = isVisibleCond
                    };
                    try
                    {
                        //TODO convert to double if float
                        pnu.Minimum = StringToInt(paraType.Tag1);
                        pnu.Maximum = StringToInt(paraType.Tag2);
                    }
                    catch
                    {
                        Debug.WriteLine("Can't convert Min/Max to Int: " + paraType.Tag1 + "/" + paraType.Tag2);
                    }
                    block.Parameters.Add(pnu);
                    break;

                case ParamTypes.Text:
                    IDynParameter pte;
                    if (para.Access == AccessType.Read)
                        pte = new Dynamic.ParamTextRead();
                    else
                        pte = new Dynamic.ParamText() { IsVisibleCondition = isVisibleCond };
                    pte.Id = para.ParameterId;
                    pte.Text = para.Text;
                    pte.SuffixText = para.SuffixText;
                    pte.Default = para.Value;
                    pte.Value = para.Value;
                    pte.Conditions = paramList;
                    pte.HasAccess = hasAccess;
                    pte.IsEnabled = IsCtlEnabled;
                    block.Parameters.Add(pte);
                    break;

                case ParamTypes.Enum:
                    List<ParamEnumOption> options = new List<ParamEnumOption>();
                    foreach(AppParameterTypeEnumViewModel enu in _context.AppParameterTypeEnums.Where(e => e.TypeId == paraType.Id).OrderBy(e => e.Order))
                    {
                        options.Add(new ParamEnumOption() { Text = enu.Text, Value = enu.Value });
                    }
                    int count = options.Count();

                    if (count > 2 || count == 1)
                    {
                        ParamEnum pen = new ParamEnum
                        {
                            Id = para.ParameterId,
                            Text = para.Text,
                            SuffixText = para.SuffixText,
                            Default = para.Value,
                            Value = para.Value,
                            Options = options,
                            Conditions = paramList,
                            HasAccess = hasAccess,
                            IsEnabled = IsCtlEnabled,
                            IsVisibleCondition = isVisibleCond
                        };
                        block.Parameters.Add(pen);
                    } else
                    {
                        Dynamic.ParamEnumTwo pent = new ParamEnumTwo
                        {
                            Id = para.ParameterId,
                            Text = para.Text,
                            SuffixText = para.SuffixText,
                            Default = para.Value,
                            Value = para.Value,
                            Option1 = options[0],
                            Option2 = options[1],
                            Conditions = paramList,
                            HasAccess = hasAccess,
                            IsEnabled = IsCtlEnabled,
                            IsVisibleCondition = isVisibleCond
                        };
                        block.Parameters.Add(pent);
                    }
                    break;

                case ParamTypes.CheckBox:
                    ParamCheckBox pch = new ParamCheckBox
                    {
                        Id = para.ParameterId,
                        Text = para.Text,
                        SuffixText = para.SuffixText,
                        Default = para.Value,
                        Value = para.Value,
                        Conditions = paramList,
                        HasAccess = hasAccess,
                        IsEnabled = IsCtlEnabled,
                        IsVisibleCondition = isVisibleCond
                    };
                    block.Parameters.Add(pch);
                    break;

                case ParamTypes.Color:
                    ParamColor pco = new ParamColor
                    {
                        Id = para.ParameterId,
                        Text = para.Text,
                        SuffixText = para.SuffixText,
                        Default = para.Value,
                        Value = para.Value,
                        Conditions = paramList,
                        HasAccess = hasAccess,
                        IsEnabled = IsCtlEnabled,
                        IsVisibleCondition = isVisibleCond
                    };
                    block.Parameters.Add(pco);
                    break;

                case ParamTypes.Time:
                    string[] tags = paraType.Tag1.Split(';');
                    ParamTime pti = new ParamTime()
                    {
                        Id = para.ParameterId,
                        Text = para.Text,
                        Default = para.Value,
                        Value = para.Value,
                        Conditions = paramList,
                        HasAccess = hasAccess,
                        IsEnabled = IsCtlEnabled,
                        Minimum = int.Parse(tags[0]),
                        Maximum = int.Parse(paraType.Tag2),
                        IsVisibleCondition = isVisibleCond
                    };

                    switch (tags[1])
                    {
                        case "Hours":
                            pti.SuffixText = "Stunden";
                            pti.Divider = 1;
                            break;
                        case "Minutes":
                            pti.SuffixText = "Minuten";
                            pti.Divider = 1;
                            break;
                        case "Seconds":
                            pti.SuffixText = "Sekunden";
                            pti.Divider = 1;
                            break;
                        case "TenSeconds":
                            pti.SuffixText = "Zehn Sekunden";
                            pti.Divider = 10;
                            break;
                        case "HundredSeconds":
                            pti.SuffixText = "Hundert Sekunden";
                            pti.Divider = 100;
                            break;
                        case "Milliseconds":
                            pti.SuffixText = "Millisekunden";
                            pti.Divider = 1;
                            break;
                        case "TenMilliseconds":
                            pti.SuffixText = "Zehn Millisekunden";
                            pti.Divider = 10;
                            break;
                        case "HundredMilliseconds":
                            pti.SuffixText = "Hundert Millisekunden";
                            pti.Divider = 100;
                            break;

                        //Todo 
                        /*
                         * PackedSecondsAndMilliseconds
                            Integer value in milliseconds
                            2 bytes:
                            (lo) lower 8 bits of milliseconds
                            (hi) ffssssss
                            (upper 2 bits of milliseconds + seconds)
                            Example: 2.500 seconds are encoded as F4h 42h
                            
                            11 110100  01000010
                            PackedDaysHoursMinutesAndSeconds
                            Integer value in seconds
                            3 bytes, same as DPT 10.001:
                            (lo) seconds
                            | minutes
                            (hi) dddhhhhh (days and hours)
                            Example: 2 days, 8 hours, 20 minutes and 10 seconds is encoded as 0Ah 14h 48h
                         */

                        default:
                            //Log.Error("TypeTime Unit nicht unterstützt!! " + tags[1]);
                            throw new Exception("TypeTime Unit nicht unterstützt!! " + tags[1]);
                    }
                    block.Parameters.Add(pti);
                    break;

                case ParamTypes.Picture:
                    ParamPicture ppic = new ParamPicture()
                    {
                        Id = para.ParameterId,
                        Text = para.Text,
                        Default = para.Value,
                        Value = para.Value,
                        Conditions = paramList,
                        HasAccess = hasAccess,
                        IsEnabled = IsCtlEnabled,
                    };
                    block.Parameters.Add(ppic);
                    break;



                default:
                    //Serilog.Log.Error("Parametertyp nicht festgelegt!! " + paraType.Type.ToString());
                    //Debug.WriteLine("Parametertyp nicht festgelegt!! " + paraType.Type.ToString());
                    throw new Exception("Parametertyp nicht festgelegt!! " + paraType.Type.ToString());
            }
        }

        private void ParseComObjects(XElement xstatic, int appId, Dictionary<string, string> args, Dictionary<string, int> idMapper) {
            XElement xobjs = xstatic.Element(GetXName("ComObjectTable"));
            XElement xrefs = xstatic.Element(GetXName("ComObjectRefs"));
            if(xobjs == null)
                xobjs = xstatic.Element(GetXName("ComObjects"));
            
            Dictionary<string, AppComObject> comObjects = new Dictionary<string, AppComObject>();

            foreach(XElement xobj in xobjs.Elements()) {
                AppComObject cobj = new AppComObject
                {
                    Id = GetItemId(xobj.Attribute("Id").Value),
                    Text = GetAttributeAsString(xobj, "Text") ?? "",
                    FunctionText = GetAttributeAsString(xobj, "FunctionText"),
                    Number = GetAttributeAsInt(xobj, "Number"),
                    ApplicationId = appId,

                    Flag_Communicate = GetAttributeAsString(xobj, "CommunicationFlag") == "Enabled",
                    Flag_Read = GetAttributeAsString(xobj, "ReadFlag") == "Enabled",
                    Flag_ReadOnInit = GetAttributeAsString(xobj, "ReadOnInitFlag") == "Enabled",
                    Flag_Transmit = GetAttributeAsString(xobj, "TransmitFlag") == "Enabled",
                    Flag_Update = GetAttributeAsString(xobj, "UpdateFlag") == "Enabled",
                    Flag_Write = GetAttributeAsString(xobj, "WriteFlag") == "Enabled"
                };

                cobj.SetSize(GetAttributeAsString(xobj, "ObjectSize"));
                cobj.SetDatapoint(GetAttributeAsString(xobj, "DatapointType"));

                if(xobj.Attribute("BaseNumber") != null) {
                    cobj.Number += int.Parse(args[GetAttributeAsString(xobj, "BaseNumber")]);
                }
                
                comObjects.Add(GetAttributeAsString(xobj, "Id"), cobj);
            }

            List<string> toImport = new List<string>();

            foreach(XElement xrcom in xstatic.Parent.Element(GetXName("Dynamic")).Descendants(GetXName("ComObjectRefRef")))
            {
                toImport.Add(xrcom.Attribute("RefId").Value);
            }


            foreach(XElement xref in xrefs.Elements()) {
                if (!toImport.Contains(xref.Attribute("Id").Value))
                {
                    Debug.WriteLine($"Nicht verwendetes KO: {xref.Attribute("Id").Value}");
                    continue;
                }

                AppComObject com = new AppComObject();
                com.LoadComp(comObjects[GetAttributeAsString(xref, "RefId")]);
                com.Id = GetItemId(GetAttributeAsString(xref, "Id"));

                if(HasAttribute(xref, "CommunicationFlag")) com.Flag_Communicate = GetAttributeAsString(xref, "CommunicationFlag") == "Enabled";
                if(HasAttribute(xref, "ReadFlag")) com.Flag_Read = GetAttributeAsString(xref, "ReadFlag") == "Enabled";
                if(HasAttribute(xref, "ReadOnInitFlag")) com.Flag_ReadOnInit = GetAttributeAsString(xref, "ReadOnInitFlag") == "Enabled";
                if(HasAttribute(xref, "TransmitFlag")) com.Flag_Transmit = GetAttributeAsString(xref, "TransmitFlag") == "Enabled";
                if(HasAttribute(xref, "UpdateFlag")) com.Flag_Update = GetAttributeAsString(xref, "UpdateFlag") == "Enabled";
                if(HasAttribute(xref, "WriteFlag")) com.Flag_Write = GetAttributeAsString(xref, "WriteFlag") == "Enabled";

                if(HasAttribute(xref, "Text")) com.Text = GetAttributeAsString(xref, "Text");
                if(HasAttribute(xref, "FunctionText")) com.FunctionText = GetAttributeAsString(xref, "FunctionText");
                if(HasAttribute(xref, "DatapointType")) com.SetDatapoint(GetAttributeAsString(xref, "DatapointType"));
                if(HasAttribute(xref, "Size")) com.SetSize(GetAttributeAsString(xref, "Size"));

                if (idMapper != null) {
                    int oldId = com.Id;
                    com.Id = counterComObjects++;
                    idMapper.Add("c" + oldId, com.Id);

                    //TODO also rename TextParameterRefID in parameterblock
                    if(xref.Attribute("TextParameterRefId") != null)
                    {
                        int id = GetItemId(xref.Attribute("TextParameterRefId").Value);
                        int newId = idMapper["p" + id];
                        xref.Attribute("TextParameterRefId").Value = "...R-" + newId;
                    }
                }



                //TODO if DataPoint is -1 !!

                com.Text = CheckForBindings(com, com.Text, xref, args, idMapper);
                Regex reg = new Regex("{{(.*)}}");
                if(com.FunctionText != null && reg.IsMatch(com.FunctionText)) {
                    //TODO maybe add binding for this too
                    Match match = reg.Match(com.FunctionText);
                    if(args.ContainsKey(match.Groups[1].Value)) {
                        com.FunctionText = com.FunctionText.Replace("{{" + match.Groups[1].Value + "}}", args[match.Groups[1].Value]);
                    }
                }

                _context.AppComObjects.Add(com);
            }

            _context.SaveChanges();
        }

        private void ParseParameter(Dictionary<int, AppParameter> parameters, Dictionary<string, string> args, XElement xpara, XElement xmem = null, int unionId = 0)
        {
            AppParameter param = new AppParameter
            {
                ParameterId = GetItemId(xpara.Attribute("Id").Value),
                Text = GetAttributeAsString(xpara, "Text"),
                Value = GetAttributeAsString(xpara, "Value"),
                UnionId = unionId,
                UnionDefault = (GetAttributeAsString(xpara, "DefaultUnionParameter") == "true" || GetAttributeAsString(xpara, "DefaultUnionParameter") == "1")
            };
            param.ParameterTypeId = parameterTypeIds[GetAttributeAsString(xpara, "ParameterType").Substring(GetAttributeAsString(xpara, "ParameterType").LastIndexOf("-") + 1)];

            string suffix = xpara.Attribute("SuffixText")?.Value;
            if (!string.IsNullOrEmpty(suffix)) param.SuffixText = suffix;

            switch (GetAttributeAsString(xpara, "Access"))
            {
                case "None":
                    param.Access = AccessType.None;
                    break;
                case "Read":
                    param.Access = AccessType.Read;
                    break;

                default:
                    param.Access = AccessType.Full;
                    break;
            }

            if (xmem != null)
            {
                if(xmem.Name.LocalName == "Memory") {
                    string codeseg = GetAttributeAsString(xmem, "CodeSegment");
                    //if(codeseg.Contains("_RS-"))
                    //    param.SegmentId = GetItemHexId(codeseg.Substring(0,27));
                    //else
                    //    param.SegmentId = GetItemHexId(codeseg.Substring(0,29));
                    param.SegmentId = GetItemHexId(codeseg);
                    param.Offset = GetAttributeAsInt(xmem, "Offset");
                    param.OffsetBit = GetAttributeAsInt(xmem, "BitOffset");
                    param.SegmentType = SegmentTypes.Memory;

                    if(xmem.Attribute("BaseOffset") != null) {
                        int offset2 = int.Parse(args[GetAttributeAsString(xmem, "BaseOffset")]);
                        param.Offset += offset2;
                    }
                } else if(xmem.Name.LocalName == "Property") {
                    param.SegmentId = (GetAttributeAsInt(xmem, "ObjectIndex") << 16) + GetAttributeAsInt(xmem, "PropertyId");
                    param.SegmentType = SegmentTypes.Property;
                    //TODO check offset??
                    //<Property ObjectIndex="0" PropertyId="78" Offset="0" BitOffset="0" />
                    //throw new Exception("Änderung nicht implementiert! Importhelper->1295 - Parameter in Property");
                } else {
                    throw new Exception("Unbekannter Speicherort: " + xmem.Name.LocalName);
                }
                
            }
            parameters.Add(param.ParameterId, param);
        }

        private void ParseParameterRefs(Dictionary<int, AppParameter> parameters, XElement refs, int appId, Dictionary<string, int> idMapper) {

            foreach(XElement xref in refs.Elements()) {
                int pId = GetItemId(xref.Attribute("Id").Value);
                AppParameter old = parameters[GetItemId(xref.Attribute("RefId").Value)];
                AppParameter final = new AppParameter();
                final.LoadPara(old);

                if (idMapper != null) {
                    pId = counterParameter++;
                    idMapper.Add("p" + GetItemId(GetAttributeAsString(xref, "Id")), pId);
                }

                final.ParameterId = pId;
                final.ApplicationId = appId;

                string text = xref.Attribute("Text")?.Value;
                final.Text = text ?? old.Text;

                string value = xref.Attribute("Value")?.Value;
                if (final.UnionDefault && value != null && value != old.Value) final.UnionDefault = false;
                final.Value = value ?? old.Value;

                AccessType access = AccessType.Null;
                switch (xref.Attribute("Access")?.Value)
                {
                    case "None":
                        access = AccessType.None;
                        break;
                    case "Read":
                        access = AccessType.Read;
                        break;
                    case "ReadWrite":
                        access = AccessType.Full;
                        break;
                }
                final.Access = access == AccessType.Null ? old.Access : access;
                _context.AppParameters.Add(final);
            }
        }
#endregion


        public List<ParamCondition> GetConditions(XElement xele)
        {
            return GetConditions(xele, false);
        }

        public List<ParamCondition> GetConditions(XElement xele, bool isParam)
        {
            //TODO also check if choose refers to param with additional chooses
            //so if it is visible
            List<ParamCondition> conds = new List<ParamCondition>();
            try
            {
                bool finished = false;
                while (true)
                {
                    xele = xele.Parent;

                    switch (xele.Name.LocalName)
                    {
                        case "when":
                            if (finished && isParam) continue;
                            ParamCondition cond = new ParamCondition();
                            int tempOut;
                            if (xele.Attribute("default")?.Value == "true")
                            {
                                //TODO check if it works
                                //check if choose ist ParameterBlock (happens when vd5 gets converted to knxprods)
                                if(xele.Parent.Parent.Name.LocalName == "ParameterBlock"){
                                    string refid = xele.Parent.Attribute("ParamRefId").Value;
                                    if(GetAttributeAsString(xele.Parent.Parent, "TextParameterRefId") == refid)
                                        break;
                                }

                                List<string> values = new List<string>();
                                IEnumerable<XElement> whens = xele.Parent.Elements();
                                foreach (XElement w in whens)
                                {
                                    if (w == xele)
                                        continue;

                                    values.AddRange(w.Attribute("test").Value.Split(' '));
                                }
                                cond.Values = string.Join(",", values);
                                cond.Operation = ConditionOperation.Default;

                                if (cond.Values == "")
                                {
                                    continue;
                                }
                            }
                            else if (xele.Attribute("test")?.Value.Contains(" ") == true || int.TryParse(xele.Attribute("test")?.Value, out tempOut))
                            {
                                cond.Values = string.Join(",", xele.Attribute("test").Value.Split(' '));
                                cond.Operation = ConditionOperation.IsInValue;
                            }
                            else if (xele.Attribute("test")?.Value.StartsWith("<") == true)
                            {
                                if (xele.Attribute("test").Value.Contains("="))
                                {
                                    cond.Operation = ConditionOperation.LowerEqualThan;
                                    cond.Values = xele.Attribute("test").Value.Substring(2);
                                }
                                else
                                {
                                    cond.Operation = ConditionOperation.LowerThan;
                                    cond.Values = xele.Attribute("test").Value.Substring(1);
                                }
                            }
                            else if (xele.Attribute("test")?.Value.StartsWith(">") == true)
                            {
                                if (xele.Attribute("test").Value.Contains("="))
                                {
                                    cond.Operation = ConditionOperation.GreatherEqualThan;
                                    cond.Values = xele.Attribute("test").Value.Substring(2);
                                }
                                else
                                {
                                    cond.Operation = ConditionOperation.GreatherThan;
                                    cond.Values = xele.Attribute("test").Value.Substring(1);
                                }
                            }
                            else if (xele.Attribute("test")?.Value.StartsWith("!=") == true)
                            {
                                cond.Operation = ConditionOperation.NotEqual;
                                cond.Values = xele.Attribute("test").Value.Substring(2);
                            }
                            else if (xele.Attribute("test")?.Value.StartsWith("=") == true)
                            {
                                cond.Operation = ConditionOperation.Equal;
                                cond.Values = xele.Attribute("test").Value.Substring(1);
                            }
                            else {
                                string attrs = "";
                                foreach(XAttribute attr in xele.Attributes())
                                {
                                    attrs += attr.Name.LocalName + "=" + attr.Value + "  ";
                                }
                                //Log.Warning("Unbekanntes when! " + attrs);
                                throw new Exception("Unbekanntes when! " + attrs);
                            }

                            cond.SourceId = GetItemId(xele.Parent.Attribute("ParamRefId").Value);
                            conds.Add(cond);
                            break;

                        case "Channel":
                        case "ParameterBlock":
                            finished = true;
                            break;

                        case "Dynamic":
                            return conds;
                    }
                }
            }
            catch(Exception e)
            {
                //Log.Error(e, "Generiere Konditionen ist fehlgeschlagen");
                throw new Exception("Generiere Konditionen ist fehlgeschlagen", e);
            }
            return conds;
        }

        private void TranslateXml(XElement xml, string selectedLang)
        {
            if (selectedLang == null) return;



            if (!xml.Descendants(XName.Get("Language", xml.Name.NamespaceName)).Any(l => l.Attribute("Identifier").Value.ToLower() == selectedLang.ToLower()))
            {
                return;
            }


            Dictionary<string, Dictionary<string, string>> transl = new Dictionary<string, Dictionary<string, string>>();

            var x = xml.Descendants(XName.Get("Language", xml.Name.NamespaceName)).Where(l => l.Attribute("Identifier").Value.ToLower() == selectedLang.ToLower());
            XElement lang = xml.Descendants(XName.Get("Language", xml.Name.NamespaceName)).Single(l => l.Attribute("Identifier").Value.ToLower() == selectedLang.ToLower());
            List<XElement> trans = lang.Descendants(XName.Get("TranslationElement", xml.Name.NamespaceName)).ToList();

            foreach (XElement translate in trans)
            {
                string id = translate.Attribute("RefId").Value;

                Dictionary<string, string> translations = new Dictionary<string, string>();

                foreach (XElement transele in translate.Elements())
                {
                    translations.Add(transele.Attribute("AttributeName").Value, transele.Attribute("Text").Value);
                }

                transl.Add(id, translations);
            }


            foreach (XElement ele in xml.Descendants())
            {
                if (ele.Attribute("Id") == null || !transl.ContainsKey(ele.Attribute("Id").Value)) continue;
                string eleId = ele.Attribute("Id").Value;


                foreach (string attr in transl[eleId].Keys)
                {
                    if (ele.Attribute(attr) != null)
                    {
                        ele.Attribute(attr).Value = transl[eleId][attr];
                    }
                    else
                    {
                        ele.Add(new XAttribute(XName.Get(attr), transl[eleId][attr]));
                    }
                }

            }
        }


        public string EscapeString(string input)
        {
            string output = "";

            foreach(char c in input)
            {
                if (c < 33) continue; //is no char

                if((c > 32 && c < 48)
                    || (c > 57 && c < 65)
                    || (c > 90 && c < 97)
                    || (c > 122))
                {
                    output += $".{((int)c):X2}";
                } else
                {
                    output += c;
                }
            }
            return output;


            input = input.Replace(".", ".2E");

            input = input.Replace(" ", ".20");
            input = input.Replace("!", ".21");
            input = input.Replace("\"", ".22");
            input = input.Replace("#", ".23");
            input = input.Replace("$", ".24");
            input = input.Replace("%", ".25");
            input = input.Replace("&", ".26");
            input = input.Replace("'", ".27");
            input = input.Replace("(", ".28");
            input = input.Replace(")", ".29");
            input = input.Replace("*", ".2A");
            input = input.Replace("+", ".2B");
            input = input.Replace(",", ".2C");
            input = input.Replace("-", ".2D");
            input = input.Replace("/", ".2F");
            input = input.Replace(":", ".3A");
            input = input.Replace(";", ".3B");
            input = input.Replace("<", ".3C");
            input = input.Replace("=", ".3D");
            input = input.Replace(">", ".3E");
            input = input.Replace("?", ".3F");
            input = input.Replace("@", ".40");
            input = input.Replace("[", ".5B");
            input = input.Replace("\\", ".5C");
            input = input.Replace("]", ".5D");
            input = input.Replace("^", ".5E");
            input = input.Replace("_", ".5F");
            input = input.Replace("{", ".7B");
            input = input.Replace("|", ".7C");
            input = input.Replace("}", ".7D");
            input = input.Replace("°", ".C2.B0");
            return input;
        }

        public string UnescapeString(string input)
        {
            string output = "";
            int i = 0;
            while(i < input.Length)
            {
                if(input[i] == '.')
                {
                    string chars = input.Substring(i + 1, 2);
                    int chari = int.Parse(chars, System.Globalization.NumberStyles.HexNumber);
                    output += (char)chari;
                    i += 3;
                } else
                {
                    output += input[i];
                    i++;
                }
            }
            return output;


            input = input.Replace(".25", "%");
            input = input.Replace(".20", " ");
            input = input.Replace(".21", "!");
            input = input.Replace(".22", "\"");
            input = input.Replace(".23", "#");
            input = input.Replace(".24", "$");
            input = input.Replace(".26", "&");
            input = input.Replace(".28", "(");
            input = input.Replace(".29", ")");
            input = input.Replace(".2B", "+");
            input = input.Replace(".2C", ",");
            input = input.Replace(".2D", "-");
            input = input.Replace(".2F", "/");
            input = input.Replace(".3A", ":");
            input = input.Replace(".3B", ";");
            input = input.Replace(".3C", "<");
            input = input.Replace(".3D", "=");
            input = input.Replace(".3E", ">");
            input = input.Replace(".3F", "?");
            input = input.Replace(".40", "@");
            input = input.Replace(".5B", "[");
            input = input.Replace(".5C", "%\\");
            input = input.Replace(".5D", "]");
            input = input.Replace(".5C", "^");
            input = input.Replace(".5F", "_");
            input = input.Replace(".7B", "{");
            input = input.Replace(".7C", "|");
            input = input.Replace(".7D", "}");
            input = input.Replace(".C2.B0", "°");

            input = input.Replace(".2E", ".");
            return input;
        }

        public bool HasAttribute(XElement ele, string attr){
            return ele.Attribute(attr) != null;
        }

        public float GetAttributeAsFloat(XElement ele, string attr)
        {
            string input = ele.Attribute(attr)?.Value;
            if (input == null) return 0;

            if (input.ToLower().Contains("e+"))
            {
                float numb = float.Parse(input.Substring(0, 5).Replace('.', ','));
                int expo = int.Parse(input.Substring(input.IndexOf('+') + 1));
                if (expo == 0)
                    return int.Parse(numb.ToString());
                float res = numb * (10 * expo);
                return res;
            }

            try
            {
                return float.Parse(input);
            }
            catch
            {
                return 0;
            }
        }

        private bool GetAttributeAsBool(XElement ele, string attr)
        {
            string val = ele.Attribute(attr)?.Value;
            return (val == "1" || val == "true");
        }

        private int GetAttributeAsInt(XElement ele, string attr)
        {
            string val = ele.Attribute(attr)?.Value;
            return int.Parse(val);
        }

        private string GetAttributeAsString(XElement ele, string attr)
        {
            return (ele.Attribute(attr) == null) ? null : ele.Attribute(attr).Value;
        }

        private int GetItemId(string id)
        {
            id = id.Substring(id.LastIndexOf("-") + 1);
            int id2;
            if (!int.TryParse(id, out id2))
                id2 = 999999;
            return id2;
        }

        private int GetItemHexId(string id)
        {
            string[] splits = id.Split('-');
            return int.Parse(splits[splits.Length - 1], System.Globalization.NumberStyles.HexNumber);
        }


        private XName GetXName(string name)
        {
            return XName.Get(name, currentNamespace);
        }

        public static int StringToInt(string input, int def = 0)
        {
            return (int)StringToFloat(input, (float)def);
        }

        public static float StringToFloat(string input, float def = 0)
        {
            if (input == null) return def;

            if (input.ToLower().Contains("e+"))
            {
                float numb = float.Parse(input.Substring(0, 5).Replace('.', ','));
                int expo = int.Parse(input.Substring(input.IndexOf('+') + 1));
                if (expo == 0)
                    return int.Parse(numb.ToString());
                float res = numb * (10 * expo);
                return res;
            }

            try
            {
                return float.Parse(input);
            }
            catch
            {
                return def;
            }
        }


        public override void Dispose()
        {
            Archive.Dispose();
        }
    }




    public class KnxProdAllocators {
        public int Index {get;set;}
        public int Increase {get;set;}
        public bool Started {get;set;} = false;
    }
}
