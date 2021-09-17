using Kaenx.DataContext.Import.Dynamic;
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

namespace Kaenx.DataContext.Import.Manager
{
    public class KnxProdFileManager : IManager, IDisposable
    {
        private ZipArchive Archive { get; set; }
        private CatalogContext _context {get;set;}
        private string currentNamespace {get;set;}
        private string appName {get;set;}

        private Dictionary<string, int> parameterTypeIds;
        private int counterUnion = 1;
        private int counterParameter = 1;
        private int counterComObjects = 0;


        public KnxProdFileManager(string path) : base(path) { }

        public override bool CheckManager()
        {
            return File.Exists(_path) && _path.EndsWith(".knxprod");
        }


        public override void Begin()
        {
            Archive = ZipFile.OpenRead(_path);
        }

        public override List<string> GetLanguages()
        {
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

            return langs;
        }

        public override List<ImportDevice> GetDeviceList()
        {
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
                ZipArchiveEntry entry = Archive.GetEntry(manName + "/Catalog.xml");
                using(Stream entryStream = entry.Open())
                {
                    XElement catXML = XDocument.Load(entryStream).Root;
                    TranslateXml(catXML, _language);
                    string ns = catXML.Name.NamespaceName;

                    IEnumerable<XElement> catalogItems = catXML.Descendants(XName.Get("CatalogItem", ns));
                    catalogItems = catalogItems.OrderBy(c => c.Attribute("Name").Value);


                    foreach(XElement item in catalogItems)
                    {
                        ImportDevice device = new ImportDevice()
                        {
                            Id = item.Attribute("Id").Value,
                            Name = item.Attribute("Name").Value,
                            Description = item.Attribute("VisibleDescription")?.Value.Replace(Environment.NewLine, " "),
                            Additional1 = item.Attribute("ProductRefId").Value,
                            Additional2 = item.Attribute("Hardware2ProgramRefId").Value
                        };
                        devices.Add(device);
                    }
                }
            }

            return devices;
        }

        public override void StartImport(List<ImportDevice> devices, CatalogContext context)
        {
            _context = context;
            UpdateKnxMaster();

            foreach(ImportDevice importDevice in devices) {
                OnProgressChanged(0);
                OnDeviceNameChanged(importDevice.Name);
                OnStateChanged("Importiere Gerät");


                
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
        }

        private void UpdateKnxMaster() {
            ZipArchiveEntry entry = Archive.GetEntry("knx_master.xml");
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
            string manu = appId.Substring(0, 6);
            ZipArchiveEntry entry = Archive.GetEntry($"{manu}/{appId}.xml");
            XElement xapp = XDocument.Load(entry.Open()).Root;
            OnStateChanged(appName + " - Übersetzen");
            TranslateXml(xapp, _language);
            xapp = xapp.Descendants(GetXName("ApplicationProgram")).ElementAt(0);
            OnStateChanged(appName + " - Infos");
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

            ImportAppSegments(xapp, app.Id);
            ImportAppParaTypes(xapp, app.Id);

            counterUnion = 1;
            counterParameter = 1;
            counterComObjects = 0;
            ImportAppStatic(xstatic, app.Id);

            if(xapp.Element(GetXName("ModuleDefs")) != null) {
                ImportModuleDefs(xapp, app.Id);
            }

            ImportDynamic(xapp.Element(GetXName("Dynamic")), app.Id);
        }

        private void ImportDynamic(XElement xdyn, int appId) {
            OnStateChanged(appName + " - Dynamic Ansicht");

            Dictionary<string, XElement> Id2Element = new Dictionary<string, XElement>();
            Dictionary<int, ParameterBlock> Id2ParamBlock = new Dictionary<int, ParameterBlock>();
            List<IDynChannel> Channels = new List<IDynChannel>();
            IDynChannel currentChannel = null;

            foreach(XElement ele in xdyn.Descendants(GetXName("ParameterBlock")))
            {
                if(ele.Attribute("Inline")?.Value == "true") continue; //Tabellen überspringen
                Id2Element.Add("pb" + GetItemId(ele.Attribute("Id").Value), ele);
            }
            foreach(XElement ele in xdyn.Descendants(GetXName("Channel")))
            {
                Id2Element.Add("ch" + GetItemId(ele.Attribute("Id").Value), ele);
            }

            using(XmlReader reader = xdyn.CreateReader()) {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.EndElement) continue;
                    string text = "";

                    switch (reader.LocalName)
                    {
                        case "ChannelIndependentBlock":
                            ChannelIndependentBlock cib = new ChannelIndependentBlock();
                            if (reader.GetAttribute("Access") == "None")
                            {
                                cib.HasAccess = false;
                                cib.IsVisible = false;
                            }
                            currentChannel = cib;
                            Channels.Add(cib);
                            break;

                        case "Channel":
                            if(reader.GetAttribute("Text") == "")
                            {
                                ChannelIndependentBlock cib2 = new ChannelIndependentBlock();
                                if (reader.GetAttribute("Access") == "None")
                                {
                                    cib2.HasAccess = false;
                                    cib2.IsVisible = false;
                                }
                                currentChannel = cib2;
                                Channels.Add(cib2);
                            } else
                            {
                                ChannelBlock cb = new ChannelBlock
                                {
                                    Id = GetItemId(reader.GetAttribute("Id")),
                                    Name = reader.GetAttribute("Name")
                                };
                                if (reader.GetAttribute("Access") == "None")
                                {
                                    cb.HasAccess = false;
                                    cb.IsVisible = false;
                                }

                                text = reader.GetAttribute("Text");

                                if (text.Contains("{{"))
                                {
                                    ParamBinding bind = new ParamBinding()
                                    {
                                        Hash = "CB:" + cb.Id
                                    };

                                    Regex reg = new Regex("{{((.+):(.+))}}");
                                    Match m = reg.Match(text);
                                    if (m.Success)
                                    {
                                        bind.DefaultText = m.Groups[3].Value;
                                        cb.DefaultText = m.Groups[3].Value;
                                        cb.Text = text.Replace(m.Value, "{{dyn}}");
                                        if (m.Groups[2].Value == "0")
                                        {
                                            string textId = reader.GetAttribute("TextParameterRefId");
                                            if (string.IsNullOrEmpty(textId)) bind.SourceId = -1;
                                            else
                                            {
                                                bind.SourceId = GetItemId(textId);
                                            }
                                        }
                                        else
                                        {
                                            string refid = m.Groups[2].Value;
                                            bind.SourceId = int.Parse(m.Groups[2].Value);
                                        }
                                    }
                                    else
                                    {
                                        reg = new Regex("{{(.+)}}");
                                        m = reg.Match(text);
                                        if (m.Success)
                                        {
                                            bind.DefaultText = "";
                                            cb.Text = text.Replace(m.Value, "{{dyn}}");
                                            if (m.Groups[1].Value == "0")
                                            {
                                                string textId = reader.GetAttribute("TextParameterRefId");
                                                if (string.IsNullOrEmpty(textId)) bind.SourceId = -1;
                                                else
                                                {
                                                    bind.SourceId = GetItemId(textId);
                                                }
                                            }
                                            else
                                            {
                                                string refid = m.Groups[2].Value;
                                                bind.SourceId = int.Parse(m.Groups[2].Value);
                                            }
                                        }
                                    }
                                }
                                else
                                    cb.Text = text;

                                cb.Conditions = GetConditions(Id2Element["ch" + cb.Id]);
                                Channels.Add(cb);
                                currentChannel = cb;
                            }
                            break;


                        case "ParameterBlock":
                            if(reader.GetAttribute("Inline") == "true") continue; //Tabellen überspringen

                            ParameterBlock pb = new ParameterBlock { Id = GetItemId(reader.GetAttribute("Id")) };
                            if (reader.GetAttribute("Access") == "None")
                            {
                                pb.HasAccess = false;
                                pb.IsVisible = false;
                            }
                            if (reader.GetAttribute("ParamRefId") != null)
                            {
                                try
                                {
                                    int paramId = GetItemId(reader.GetAttribute("ParamRefId"));
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
                                text = reader.GetAttribute("Text");

                            if (text?.Contains("{{") == true)
                            {
                                ParamBinding bind = new ParamBinding()
                                {
                                    Hash = "PB:" + pb.Id
                                };

                                Regex reg = new Regex("{{((.+):(.+))}}");
                                Match m = reg.Match(text);
                                if (m.Success)
                                {
                                    bind.DefaultText = m.Groups[3].Value;
                                    pb.DefaultText = m.Groups[3].Value;
                                    pb.Text = text.Replace(m.Value, "{{dyn}}");
                                    if (m.Groups[2].Value == "0")
                                    {
                                        string textId = reader.GetAttribute("TextParameterRefId");
                                        if (string.IsNullOrEmpty(textId)) bind.SourceId = -1;
                                        else
                                        {
                                            bind.SourceId = GetItemId(textId);
                                        }
                                    }
                                    else
                                    {
                                        bind.SourceId = int.Parse(m.Groups[2].Value);
                                    }
                                }
                                else
                                {
                                    reg = new Regex("{{(.+)}}");
                                    m = reg.Match(text);
                                    if (m.Success)
                                    {
                                        bind.DefaultText = "";
                                        pb.Text = text.Replace(m.Value, "{{dyn}}");
                                        if (m.Groups[1].Value == "0")
                                        {
                                            string textId = reader.GetAttribute("TextParameterRefId");
                                            if (string.IsNullOrEmpty(textId)) bind.SourceId = -1;
                                            else
                                            {
                                                bind.SourceId = GetItemId(textId);
                                            }
                                        }
                                        else
                                        {
                                            bind.SourceId = int.Parse(m.Groups[1].Value);
                                        }
                                    }
                                }
                            }
                            else
                                pb.Text = text;

                            pb.Conditions = GetConditions(Id2Element["pb" + pb.Id]);
                            currentChannel.Blocks.Add(pb);
                            Id2ParamBlock.Add(pb.Id, pb);
                            break;

                        case "Assign":
                        case "choose":
                        case "when":
                        case "Dynamic":
                        case "ParameterRefRef":
                        case "ParameterSeparator":
                        case "ComObjectRefRef":
                            break;

                        default:
                            //Log.Warning("Unbekanntes Element in Dynamic: " + reader.LocalName);
                            System.Diagnostics.Debug.WriteLine("Unbekanntest Element in Dynamic: " + reader.LocalName);
                            break;
                    }
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
        }

        public void RenameDynamic(XElement xdyn, Dictionary<string, string> args, Dictionary<string, int> idMapper) {
            List<XElement> xobjs = new List<XElement>();
            xobjs.AddRange(xdyn.Descendants(GetXName("ParameterRefRef")));
            xobjs.AddRange(xdyn.Descendants(GetXName("choose")));

            foreach(XElement xref in xobjs){
                int id = 0;
                
                if(xref.Attribute("RefId") != null)
                {
                    id = GetItemId(xref.Attribute("RefId").Value);
                    int newId = idMapper["p"+id];
                    xref.Attribute("RefId").Value = "xx_R-" + newId;
                }
                else
                {
                    id = GetItemId(xref.Attribute("ParamRefId").Value);
                    int newId = idMapper["p"+id];
                    xref.Attribute("ParamRefId").Value = "xx_R-" + newId;
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
            
            Regex reg = new Regex("{{(.*)}}");

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
                    xobj.Attribute("Id").Value = xobj.Attribute("Id").Value.Replace(numb, args[numb]);
                }

                if(xobj.Attribute("ParamRefId") != null) {
                    int id = GetItemId(xobj.Attribute("ParamRefId").Value);
                    int newId = idMapper["p"+id];
                    xobj.Attribute("ParamRefId").Value = "xx_R-" + newId;
                }
            }
        }

        private Dictionary<string, int> ImportAppStatic(XElement xstatic, int appId, Dictionary<string, string> args = null) {
            if(args == null) OnStateChanged(appName + " - Parameter");
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





            if(args == null) OnStateChanged(appName + " - Kommunikationsobjekte");
            if(xstatic.Element(GetXName("ComObjects")) != null) {
                ParseComObjects(xstatic.Element(GetXName("ComObjects")), xstatic.Element(GetXName("ComObjectRefs")), appId, args, idMapper);
            }


            _context.SaveChanges();
            return idMapper;
        }

        private void ImportAppSegments(XElement xapp, int appId) {
            OnStateChanged(appName + " - Segments");
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
                        aas.SegmentId = GetItemHexId(GetAttributeAsString(seg, "Id").Substring(0,29));
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
                        ars.SegmentId = GetItemHexId(GetAttributeAsString(seg, "Id").Substring(0,27));
                        ars.Offset = GetAttributeAsInt(seg, "Offset");
                        ars.Size = GetAttributeAsInt(seg, "Size");
                        ars.LsmId = GetAttributeAsInt(seg, "LoadStateMachine");
                        _context.AppSegments.Add(ars);
                        break;

                    default:
                        //msg = "Unbekanntes Segment: " + seg.Name.LocalName;
                        //if (!Errors.Contains(msg))
                        //    Errors.Add(msg);
                        //TODO Log
                        break;
                }
            }
            _context.SaveChanges();
        }

        private void ImportAppParaTypes(XElement xapp, int appId) {
            OnStateChanged(appName + " - ParamTypes");
            parameterTypeIds = new Dictionary<string, int>();
            IEnumerable<XElement> xparaTypes = xapp.Descendants(GetXName("ParameterType"));

            foreach(XElement xparaType in xparaTypes) {
                AppParameterTypeViewModel model = new AppParameterTypeViewModel() {
                    Name = GetAttributeAsString(xparaType, "Name"),
                    ApplicationId = appId
                };

                bool modelAdded = false;
                XElement child = xparaType.Elements().ElementAt(0);

                switch(child.Name.LocalName) {
                    case "TypeNumber":
                        if (child.Attribute("UIHint")?.Value == "CheckBox")
                        {
                            model.Type = ParamTypes.CheckBox;
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
                                    break;
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
                                TypeId = model.Id,
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
                        break;
                }
                if(!modelAdded) _context.AppParameterTypes.Add(model);
            }
            _context.SaveChanges();


            foreach(AppParameterTypeViewModel model in _context.AppParameterTypes.Where(pt => pt.ApplicationId == appId)) {
                parameterTypeIds.Add(EscapeString(model.Name), model.Id);
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
            catch (Exception e)
            {
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
        private void ParseComObjects(XElement xobjs, XElement xrefs, int appId, Dictionary<string, string> args, Dictionary<string, int> idMapper) {
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

            foreach(XElement xref in xrefs.Elements()) {
                AppComObject com = new AppComObject();
                com.LoadComp(comObjects[GetAttributeAsString(xref, "RefId")]);

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

                if(idMapper != null) {
                    int oldId = com.Id;
                    com.Id = counterComObjects++;
                    idMapper.Add("c" + oldId, com.Id);
                }



                //TODO if DataPoint is -1 !!

                Regex reg = new Regex("{{(.*)}}");
                if(reg.IsMatch(com.Text)){
                    Match match = reg.Match(com.Text);
                    string g2 = match.Groups[2].Value;
                    if(args != null && args.ContainsKey(g2)) {
                        //Argument von Modul einsetzen
                        com.Text = com.Text.Replace("{{" + g2 + "}}", args[g2]);
                    } else {
                        //Text beinhaltet ein Binding zu einem Parameter
                        if(g2.Contains(":")){
                            string[] opts = g2.Split(":");
                            com.BindedId = opts[0] == "0" ? -1 : int.Parse(opts[0]);
                            com.BindedDefaultText = opts[1];
                        } else {
                            com.BindedId = g2 == "0" ? -1 : int.Parse(g2);
                            com.BindedDefaultText = "";
                        }
                        //If we are in a ModuleDefine we have to map to the new Id
                        if(args != null)
                            com.BindedId = idMapper["p" + com.BindedId];
                    }
                }
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

        private void ParseParameter(Dictionary<int, AppParameter> parameters, Dictionary<string, string> args, XElement xpara, XElement xmem = null, int unionId = 0) {
            AppParameter param = new AppParameter
            {
                ParameterId = GetItemId(xpara.Attribute("Id").Value),
                Text = GetAttributeAsString(xpara, "Text"),
                Value = GetAttributeAsString(xpara, "Value"),
                UnionId = unionId,
                UnionDefault = (GetAttributeAsString(xpara, "DefaultUnionParameter") == "true" || GetAttributeAsString(xpara, "DefaultUnionParameter") == "1")
            };
            param.ParameterTypeId = parameterTypeIds[GetAttributeAsString(xpara, "ParameterType").Substring(GetAttributeAsString(xpara, "ParameterType").LastIndexOf("-")+1)];

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
                    if(codeseg.Contains("_RS-"))
                        param.SegmentId = GetItemHexId(codeseg.Substring(0,27));
                    else
                        param.SegmentId = GetItemHexId(codeseg.Substring(0,29));
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
                    throw new Exception("Änderung nicht implementiert! Importhelper->1295 - Parameter in Property");
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

                if(idMapper != null) {
                    pId = counterParameter++;
                    idMapper.Add("p" + GetItemId(GetAttributeAsString(xref, "RefId")), pId);
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
            return GetConditions(xele, false).paramList;
        }

        public (List<ParamCondition> paramList, string hash) GetConditions(XElement xele, bool isParam)
        {
            List<ParamCondition> conds = new List<ParamCondition>();
            try
            {
                string ids = xele.Attribute("RefId")?.Value ?? "";
                if (ids == "" && xele.Attribute("Id") != null) ids = xele.Attribute("Id").Value;

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
                                ids = "d" + ids;
                                List<string> values = new List<string>();
                                IEnumerable<XElement> whens = xele.Parent.Elements();
                                foreach (XElement w in whens)
                                {
                                    if (w == xele)
                                        continue;

                                    values.AddRange(w.Attribute("test").Value.Split(" "));
                                }
                                cond.Values = string.Join(",", values);
                                cond.Operation = ConditionOperation.Default;

                                if (cond.Values == "")
                                {
                                    ids = "|" + xele.Parent.Attribute("ParamRefId").Value + ".|" + ids;
                                    continue;
                                }
                            }
                            else if (xele.Attribute("test")?.Value.Contains(" ") == true || int.TryParse(xele.Attribute("test")?.Value, out tempOut))
                            {
                                cond.Values = string.Join(",", xele.Attribute("test").Value.Split(" "));
                                cond.Operation = ConditionOperation.IsInValue;
                            }
                            else if (xele.Attribute("test")?.Value.StartsWith("!=") == true)
                            {
                                cond.Values = xele.Attribute("test").Value.Substring(2);
                                cond.Operation = ConditionOperation.NotEqual;
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

                            ids = "|" + cond.SourceId + "." + cond.Values + "|" + ids;
                            break;

                        case "Channel":
                        case "ParameterBlock":
                            ids = xele.Attribute("Id").Value + "|" + ids;
                            finished = true;
                            break;

                        case "Dynamic":
                            return (conds, Convert.ToBase64String(Encoding.UTF8.GetBytes(ids)));
                    }
                }
            }
            catch(Exception e)
            {
                //Log.Error(e, "Generiere Konditionen ist fehlgeschlagen");
                throw new Exception("Generiere Konditionen ist fehlgeschlagen", e);
            }
            return (conds, "");
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


        public string EscapeString(string input) {
            input = input.Replace(" ", "%20");
            input = input.Replace("&", "%26");
            input = input.Replace("+", "%2B");
            input = input.Replace("-", "%2D");
            input = input.Replace(".", "%2E");
            input = input.Replace("/", "%2F");
            input = input.Replace(":", "%3A");
            input = input.Replace(";", "%3B");
            input = input.Replace("=", "%3D");
            input = input.Replace("?", "%3F");
            input = input.Replace("_", "%5F");

            input = input.Replace("%", ".");
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
            return int.Parse(id.Substring(id.LastIndexOf("-") + 1));
        }

        private int GetItemHexId(string id)
        {
            return int.Parse(id.Substring(id.LastIndexOf("-") + 1), System.Globalization.NumberStyles.HexNumber);
        }


        private XName GetXName(string name)
        {
            return XName.Get(name, currentNamespace);
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
