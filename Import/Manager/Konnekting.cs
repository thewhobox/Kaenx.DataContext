using Kaenx.DataContext.Catalog;
using Kaenx.DataContext.Import.Dynamic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Globalization;

namespace Kaenx.DataContext.Import.Manager
{
    public class Konnekting : IManager
    {

        private XElement kDevice { get; set; }
        private CatalogContext _context { get; set; }


        public Konnekting(string path) : base(path) 
        {
            kDevice = XDocument.Load(path).Root;
        }
        
        public override bool CheckManager()
        {
            return File.Exists(_path) && _path.EndsWith(".kdevice.xml");
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

        public override void StartImport(List<ImportDevice> ids, CatalogContext context)
        {
            _context = context;
            XElement devices = kDevice.Element(GetXName("Device")).Element(GetXName("Parameters"));

            foreach(XElement xdevice in kDevice.Elements(GetXName("Device")))
            {
                int manuId = int.Parse(xdevice.Attribute("ManufacturerId").Value, System.Globalization.NumberStyles.HexNumber);
                string manuName = xdevice.Element(GetXName("ManufacturerName")).Value;

                if(!context.Manufacturers.Any(m => m.ImportType == ImportTypes.Konnekting && m.ManuId == manuId)){
                    ManufacturerViewModel manu = new ManufacturerViewModel() {
                        ImportType = ImportTypes.Konnekting,
                        ManuId = manuId,
                        Name = manuName
                    };
                    context.Manufacturers.Add(manu);
                    context.SaveChanges();
                    manuId = manu.Id;
                } else {
                    ManufacturerViewModel manu = context.Manufacturers.Single(m => m.ImportType == ImportTypes.Konnekting && m.ManuId == manuId);
                    manuId = manu.Id;
                }

                if (ids.Any(id => id.Id == xdevice.Attribute("DeviceId").Value))
                {
                    OnDeviceNameChanged(xdevice.Element(GetXName("DeviceName")).Value);
                    OnStateChanged("Allgemeine Infos");

                    string hardNumber = xdevice.Attribute("DeviceId").Value;
                    int hardVersion = int.Parse(xdevice.Attribute("Revision").Value);
                    bool sectionExists = context.Sections.Any(s => s.ParentId == -1 && s.ImportType == ImportTypes.Konnekting && s.Name == manuName);
                    bool hardwareExists = context.Hardware2App.Any(s => s.ManuId == manuId && s.Number == hardNumber && s.Version == hardVersion);
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
                        //TODO add manu and ImportType
                        Hardware2AppModel hard = context.Hardware2App.Single(s => s.ManuId == manuId && s.Number == hardNumber && s.Version == hardVersion);
                        hardwareId = hard.Id;
                    } else
                    {
                        //TODO add manu and ImportType
                        Hardware2AppModel hard = new Hardware2AppModel()
                        {
                            Number = hardNumber,
                            Version = hardVersion,
                            ManuId = manuId
                        };
                        context.Hardware2App.Add(hard);
                        context.SaveChanges();
                        hardwareId = hard.Id;
                    }
                    

                    ApplicationViewModel app = null;
                    if(context.Applications.Any(a => a.Manufacturer == manuId && a.HardwareId == hardwareId)){
                        app = context.Applications.Single(a => a.Manufacturer == manuId && a.HardwareId == hardwareId);
                    } else {
                        app = new ApplicationViewModel();
                        app.Name = xdevice.Element(GetXName("DeviceName")).Value;
                        app.HardwareId = hardwareId;
                        app.Manufacturer = manuId;
                        app.LoadProcedure = LoadProcedureTypes.Konnekting;
                        app.Number = int.Parse(hardNumber);
                        app.Version = hardVersion;
                        context.Applications.Add(app);
                        context.SaveChanges();
                    }

                    bool addsExists = context.AppAdditionals.Any(a => a.ApplicationId == app.Id);
                    AppAdditional adds = null;
                    if(addsExists){
                        adds = context.AppAdditionals.Single(a => a.ApplicationId == app.Id);
                    } else {
                        adds = new AppAdditional() {
                            ApplicationId = app.Id
                        };
                        adds.Bindings = FunctionHelper.ObjectToByteArray(new List<int>());
                        adds.Assignments = FunctionHelper.ObjectToByteArray(new List<int>());
                    }

                //TODO check if device exists
                    DeviceViewModel model = new DeviceViewModel();
                    model.Name = app.Name;
                    model.HasIndividualAddress = true;
                    model.HasApplicationProgram = true;
                    model.IsPowerSupply = false;
                    model.IsCoupler = false;
                    model.CatalogId = sectionId;
                    model.HardwareId = hardwareId;
                    model.ImportType = ImportTypes.Konnekting;
                    model.ManufacturerId = manuId;
                    context.Devices.Add(model);

                    Dictionary<int, string> Id2Values = new Dictionary<int, string>();
                    List<IDynChannel> Channels = new List<IDynChannel>();
                    ChannelIndependentBlock channel = new ChannelIndependentBlock() {  IsVisible = true, HasAccess = true };
                    Channels.Add(channel);


                    OnStateChanged("Parameter");

                    foreach(XElement group in xdevice.Element(GetXName("Parameters")).Elements(GetXName("ParameterGroup")))
                    {
                        ParameterBlock block = new ParameterBlock();
                        block.HasAccess = true;
                        block.Conditions = GenerateConditionsParameterGroup(int.Parse(group.Attribute("Id").Value), xdevice);
                        block.Text = group.Attribute("Name").Value;

                        foreach(XElement xpara in group.Elements())
                        {
                            int paraId = int.Parse(xpara.Attribute("Id").Value);
                            bool paraExists = context.AppParameters.Any(p => p.ApplicationId == app.Id && p.ParameterId == paraId);
                            AppParameter para = null;

                            if(paraExists) {
                                para = context.AppParameters.Single(p => p.ApplicationId == app.Id && p.ParameterId == paraId);
                            } else { 
                                para = new AppParameter();
                                para.Access = AccessType.Full;
                                para.ParameterId = paraId;
                                para.Text = xpara.Element(GetXName("Description")).Value;
                                para.ApplicationId = app.Id;
                            }
                            
                            XElement xvalue = xpara.Element(GetXName("Value"));
                            para.Value = HexToString(xvalue.Attribute("Default").Value);

                            Id2Values.Add(para.ParameterId, para.Value);

                            List<ParamCondition> _conditions = GenerateConditionsParameter(para.ParameterId, xdevice);
                            AppParameterTypeViewModel paraType = GenerateParameterType(app.Id, para.ParameterId, xvalue);
                            //TODO save paraType or update
                            para.ParameterTypeId = paraType.Id;


                            string xtype = xpara.Element(GetXName("Value")).Attribute("Type").Value;
                            if (xvalue.Attribute("Options").Value.Contains("|"))
                            {
                                string[] options = xvalue.Attribute("Options").Value.Split('|');
                                int order = 0;

                                if(!paraExists) {
                                    foreach(string option in options){
                                        string[] opts = option.Split('=');

                                        AppParameterTypeEnumViewModel typeEnum = new AppParameterTypeEnumViewModel();
                                        typeEnum.TypeId = paraType.Id;
                                        //TODO check if parameterId is needed
                                        typeEnum.Text = opts[1];
                                        typeEnum.Value = HexToString(opts[0]);
                                        typeEnum.Order = order++;
                                        context.AppParameterTypeEnums.Add(typeEnum);
                                    }
                                }

                                if(options.Length == 2)
                                {
                                    ParamEnumTwo pet = new ParamEnumTwo() { 
                                        Text = para.Text, 
                                        Id = para.ParameterId,
                                        Conditions = _conditions,
                                        HasAccess = true
                                    };
                                    string[] opt1 = options[0].Split('=');
                                    pet.Option1 = new ParamEnumOption() { Text = opt1[1], Value = HexToString(opt1[0]) };
                                    string[] opt2 = options[1].Split('=');
                                    pet.Option2 = new ParamEnumOption() { Text = opt2[1], Value = HexToString(opt2[0]) };
                                    block.Parameters.Add(pet);
                                } else
                                {
                                    ParamEnum pe = new ParamEnum() { 
                                        Text = para.Text,
                                        Id = para.ParameterId,
                                        Conditions = _conditions,
                                        HasAccess = true
                                    };
                                    foreach (string option in options)
                                    {
                                        string[] opts = option.Split('=');
                                        ParamEnumOption opt = new ParamEnumOption();
                                        opt.Text = opts[1];
                                        opt.Value = HexToString(opts[0]);

                                        pe.Options.Add(opt);
                                    }
                                    block.Parameters.Add(pe);
                                }
                            } else if(xtype == "string11") {
                                para.Value = System.Text.Encoding.ASCII.GetString(HexToBytes(xvalue.Attribute("Default").Value));

                                ParamText pt = new ParamText();
                                pt.Id = para.ParameterId;
                                pt.Conditions = _conditions;
                                pt.HasAccess = true;
                                pt.Default = para.Value;
                                pt.MaxLength = 11;
                                block.Parameters.Add(pt);
                            } else if(xtype.StartsWith("raw")) {
                                //TODO add support for raw
                            } else {
                                ParamNumber pn = new ParamNumber();
                                pn.Id = para.ParameterId;
                                pn.Conditions = _conditions;
                                pn.Text = para.Text;
                                pn.Default = para.Value;
                                pn.Minimum = int.Parse(paraType.Tag1);
                                pn.Maximum = int.Parse(paraType.Tag2);
                                block.Parameters.Add(pn);
                            }


                            if(paraExists)
                                context.AppParameters.Update(para);
                            else
                                context.AppParameters.Add(para);
                        }


                        channel.Blocks.Add(block);
                    }

                    CalcDefaultVisibility(channel, Id2Values);
                    adds.ParamsHelper = FunctionHelper.ObjectToByteArray(Channels, true, "Kaenx.DataContext.Import.Dynamic");
                    
                    OnStateChanged("Kommunikationsobjekte");
                    List<int> comsDefault = new List<int>();
                    foreach(XElement xcom in xdevice.Element(GetXName("CommObjects")).Elements()) {
                        AppComObject com = null;
                        int comNumber = int.Parse(xcom.Attribute("Id").Value);
                        bool comExists = context.AppComObjects.Any(c => c.ApplicationId == app.Id && c.Number == comNumber);
                        if(comExists)
                            com = context.AppComObjects.Single(c => c.ApplicationId == app.Id && c.Number == comNumber);
                        else {
                            com = new AppComObject();
                        }

                        com.Id = comNumber;
                        com.Number = comNumber;
                        com.Text = xcom.Element(GetXName("Name")).Value;
                        com.FunctionText = xcom.Element(GetXName("Function")).Value;
                        string[] dpt = xcom.Element(GetXName("DataPointType")).Value.Split('.');
                        com.Datapoint = int.Parse(dpt[0]);
                        com.DatapointSub = int.Parse(dpt[1]);
                        com.Size = 1;
                        //TODO get real size!
                        byte flagByte = Convert.ToByte(int.Parse(xcom.Element(GetXName("Flags")).Value));
                        BitArray flags = new BitArray(new byte[] { flagByte });

                        com.Flag_ReadOnInit = flags.Get(0);
                        com.Flag_Update = flags.Get(1);
                        com.Flag_Transmit = flags.Get(2);
                        com.Flag_Write = flags.Get(3);
                        com.Flag_Read = flags.Get(4);
                        com.Flag_Communicate = flags.Get(5);

                        if(comExists)
                            context.AppComObjects.Update(com);
                        else
                            context.AppComObjects.Add(com);

                        List<ParamCondition> conds = GenerateConditionsComObjects(com.Id, xdevice);
                        bool isVisible = FunctionHelper.CheckConditions(conds, Id2Values);
                        if(isVisible)
                            comsDefault.Add(com.Id);
                    }

                    adds.ComsDefault = FunctionHelper.ObjectToByteArray(comsDefault);


                    if(addsExists)
                        context.AppAdditionals.Update(adds);
                    else
                        context.AppAdditionals.Add(adds);
                }
            }

            context.SaveChanges();

            OnDeviceNameChanged("Fertig");
            OnStateChanged("Abgeschlossen");
        }


        private List<ParamCondition> GenerateConditionsParameter(int eleId, XElement xdevice) {
            return GenerateConditions(eleId, xdevice, "Parameter", "Param");
        }

        private List<ParamCondition> GenerateConditionsParameterGroup(int eleId, XElement xdevice) {
            return GenerateConditions(eleId, xdevice, "ParameterGroup", "ParamGroup");
        }

        private List<ParamCondition> GenerateConditionsComObjects(int eleId, XElement xdevice) {
            return GenerateConditions(eleId, xdevice, "CommObject", "CommObj");
        }

        private List<ParamCondition> GenerateConditions(int paramId, XElement xdevice, string type, string attr) {
            List<ParamCondition> conds = new List<ParamCondition>();

            var paramDependencies = xdevice.Element(GetXName("Dependencies"))?.Elements(GetXName(type + "Dependency")).Where(d => d.Attribute(attr + "Id").Value == paramId.ToString());
            if(paramDependencies == null) return conds;

            foreach(XElement dep in paramDependencies){
                ParamCondition cond = new ParamCondition();
                cond.SourceId = int.Parse(dep.Attribute("TestParamId").Value);
                cond.Values = HexToString(dep.Attribute("TestValue").Value);

                switch(dep.Attribute("Test").Value) {
                    case "eq":
                        cond.Operation = ConditionOperation.Equal;
                        break;

                    case "ne":
                        cond.Operation = ConditionOperation.NotEqual;
                        break;

                    case "gt":
                        cond.Operation = ConditionOperation.GreatherThan;
                        break;

                    case "lt":
                        cond.Operation = ConditionOperation.LowerThan;
                        break;

                    case "ge":
                        cond.Operation = ConditionOperation.GreatherEqualThan;
                        break;

                    case "le":
                        cond.Operation = ConditionOperation.LowerEqualThan;
                        break;
                    
                    default:
                        throw new Exception("Unknown Condition Type: " + dep.Attribute("Test").Value);
                }

                conds.Add(cond);
            }


            return conds;
        }

        private AppParameterTypeViewModel GenerateParameterType(int appId, int paraId, XElement xvalue) {
            string xtype = xvalue.Attribute("Type").Value;
            AppParameterTypeViewModel paraType = null;

            if(xtype == "string11")
            {
                if(_context.AppParameterTypes.Any(t => t.ApplicationId == appId && t.Name == "string11"))
                {
                    paraType = _context.AppParameterTypes.Single(t => t.ApplicationId == appId && t.Name == "string11");
                    return paraType;
                } else {
                    paraType = new AppParameterTypeViewModel();
                    paraType.ApplicationId = appId;
                    paraType.Name = "string11";
                }
            } else if(xtype.StartsWith("raw")){
                if(_context.AppParameterTypes.Any(t => t.ApplicationId == appId && t.Name == xtype))
                {
                    paraType = _context.AppParameterTypes.Single(t => t.ApplicationId == appId && t.Name == xtype);
                    return paraType;
                } else {
                    paraType = new AppParameterTypeViewModel();
                    paraType.ApplicationId = appId;
                    paraType.Name = xtype;
                }
            } else if(string.IsNullOrEmpty(xvalue.Attribute("Options").Value)) {
                string min = HexToString(xvalue.Attribute("Min").Value);
                string max = HexToString(xvalue.Attribute("Max").Value);

                if(_context.AppParameterTypes.Any(t => t.ApplicationId == appId && t.Name == $"{xtype}-{min}-{max}"))
                {
                    paraType = _context.AppParameterTypes.Single(t => t.ApplicationId == appId && t.Name == $"{xtype}-{min}-{max}");
                    return paraType;
                } else {
                    paraType = new AppParameterTypeViewModel();
                    paraType.ApplicationId = appId;
                    paraType.Name = $"{xtype}-{min}-{max}";
                }
            } else {
                if(_context.AppParameterTypes.Any(t => t.ApplicationId == appId && t.Name == $"enum-{paraId}"))
                {
                    paraType = _context.AppParameterTypes.Single(t => t.ApplicationId == appId && t.Name == $"enum-{paraId}");
                    return paraType;
                } else {
                    paraType = new AppParameterTypeViewModel();
                    paraType.ApplicationId = appId;
                    paraType.Name = $"enum-{paraId}";
                }
            }


            switch(xtype){
                case "uint8":
                    paraType.Type = ParamTypes.NumberUInt;
                    paraType.Size = 8;
                    break;
                case "int8":
                    paraType.Type = ParamTypes.NumberInt;
                    paraType.Size = 8;
                    break;
                case "uint16":
                    paraType.Type = ParamTypes.NumberUInt;
                    paraType.Size = 16;
                    break;
                case "int16":
                    paraType.Type = ParamTypes.NumberInt;
                    paraType.Size = 16;
                    break;
                case "uint32":
                    paraType.Type = ParamTypes.NumberUInt;
                    paraType.Size = 32;
                    break;
                case "int32":
                    paraType.Type = ParamTypes.NumberInt;
                    paraType.Size = 32;
                    break;
                case "raw1":
                case "raw2":
                case "raw3":
                case "raw4":
                case "raw5":
                case "raw6":
                case "raw7":
                case "raw8":
                case "raw9":
                case "raw10":
                case "raw11":
                    paraType.Type = ParamTypes.NumberHex;
                    paraType.Size = 8 * int.Parse(xtype.Substring(3));
                    break;
                case "string11":
                    paraType.Type = ParamTypes.Text;
                    paraType.Size = 8 * 11;
                    break;
            }

            if(string.IsNullOrEmpty(xvalue.Attribute("Options").Value) && (xtype.StartsWith("uint") || xtype.StartsWith("int"))){
                paraType.Tag1 = HexToString(xvalue.Attribute("Min").Value);
                paraType.Tag2 = HexToString(xvalue.Attribute("Max").Value);
            }
            if(!string.IsNullOrEmpty(xvalue.Attribute("Options").Value))
            {
                paraType.Type = ParamTypes.Enum;
            }

            _context.AppParameterTypes.Add(paraType);
            _context.SaveChanges();
            return paraType;
        }

        private void CalcDefaultVisibility(ChannelIndependentBlock channel, Dictionary<int, string> values){
            foreach(ParameterBlock block in channel.Blocks) {
                block.IsVisible = FunctionHelper.CheckConditions(block.Conditions, values);

                foreach(IDynParameter para in block.Parameters){
                    para.IsVisible = FunctionHelper.CheckConditions(para.Conditions, values);
                }
            }
        }

        private string HexToString(string input)
        {
            return int.Parse(input, System.Globalization.NumberStyles.HexNumber).ToString();
        }

        public static byte[] HexToBytes(string hexString)
        {
            if (hexString.Length % 2 != 0)
            {
                throw new ArgumentException("The binary key cannot have an odd number of digits: " + hexString.Length);
            }

            byte[] data = new byte[hexString.Length / 2];
            for (int index = 0; index < data.Length; index++)
            {
                string byteValue = hexString.Substring(index * 2, 2);
                data[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return data; 
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
