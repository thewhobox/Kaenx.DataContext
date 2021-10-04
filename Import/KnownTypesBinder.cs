using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Serialization;


public class KnownTypesBinder : ISerializationBinder
{
    public Dictionary<string, Type> KnownTypes { get; set; } = new Dictionary<string, Type>();
    public Dictionary<string, string> TypeMapper = new Dictionary<string, string>() {
        {"ChannelIndependentBlock", "1"},
        {"ChannelBlock", "2"},
        {"ParameterBlock", "3"},
        {"ParameterTable", "4"},
        {"ParamCheckBox", "5"},
        {"ParamColor", "6"},
        {"ParamEnum", "7"},
        {"ParamEnumOption", "8"},
        {"ParamEnumTwo", "9"},
        {"ParamNone", "0"},
        {"ParamNumber", "a"},
        {"ParamSeperator", "b"},
        {"ParamSeperatorBox", "c"},
        {"ParamText", "d"},
        {"ParamTextRead", "e"},
        {"ParamTime", "f"},
        {"AssignParameter", "g"},
        {"ParamBinding", "h"},
        {"ParamCondition", "i"},
        {"TablePosition", "j"},
        {"TableColumn", "k"},
        {"TableRow", "l"},
        {"ViewParamModel", "m"}
    };
    
    public KnownTypesBinder(string ns) {
        var q = from t in Assembly.GetExecutingAssembly().GetTypes()
                where t.IsClass && t.IsNested == false && t.Namespace == ns
                select t;

        foreach (Type t in q.ToList())
        {
            KnownTypes.Add(TypeMapper[t.Name], t);
        }
    }


    public Type BindToType(string assemblyName, string typeName)
    {
        return KnownTypes[typeName];
    }

    public void BindToName(Type serializedType, out string assemblyName, out string typeName)
    {
        assemblyName = null;
        typeName = TypeMapper[serializedType.Name];
    }
}