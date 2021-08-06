using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Serialization;


public class KnownTypesBinder : ISerializationBinder
{
    public Dictionary<string, Type> KnownTypes { get; set; } = new Dictionary<string, Type>();

    
    public KnownTypesBinder(string ns) {
        var q = from t in Assembly.GetExecutingAssembly().GetTypes()
                where t.IsClass && t.IsNested == false && t.Namespace == ns
                select t;

        foreach (Type t in q.ToList())
        {
            KnownTypes.Add(t.Name, t);
        }
    }


    public Type BindToType(string assemblyName, string typeName)
    {
        return KnownTypes[typeName];
    }

    public void BindToName(Type serializedType, out string assemblyName, out string typeName)
    {
        assemblyName = null;
        typeName = serializedType.Name;
    }
}