using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Text;
using Kaenx.DataContext.Import.Dynamic;

namespace Kaenx.DataContext.Import {

    public class FunctionHelper {

        public static byte[] ObjectToByteArray(object obj, string ns = null)
        {
            string text;

            if (ns != null)
            {
                KnownTypesBinder knownTypesBinder = new KnownTypesBinder(ns);

                text = Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.None, new Newtonsoft.Json.JsonSerializerSettings
                {
                    TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects,
                    SerializationBinder = knownTypesBinder,
                    Formatting = Newtonsoft.Json.Formatting.None
                });
            }
            else
            {
                text = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
            }
            return Zip(text); //System.Text.Encoding.UTF8.GetBytes(text);
        }

        public static T ByteArrayToObject<T>(byte[] obj, string ns = null)
        {
            string text = Unzip(obj); //System.Text.Encoding.UTF8.GetString(obj);

            if (ns != null)
            {
                KnownTypesBinder knownTypesBinder = new KnownTypesBinder(ns);

                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(text, new Newtonsoft.Json.JsonSerializerSettings
                {
                    TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects,
                    SerializationBinder = knownTypesBinder,
                    Formatting = Newtonsoft.Json.Formatting.None
                });
            }
            else
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(text);
            }

        }

        public static string Unzip(byte[] bytes) {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream()) {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress)) {
                    gs.CopyTo(mso);
                }

                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }

        public static byte[] Zip(string str) {
            var bytes = Encoding.UTF8.GetBytes(str);

            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream()) {
                using (var gs = new GZipStream(mso, CompressionMode.Compress)) {
                    msi.CopyTo(gs);
                }

                return mso.ToArray();
            }
        }

        public static bool CheckConditions(List<ParamCondition> conds, Dictionary<int, string> values)
        {
            bool flag = true;

            foreach (ParamCondition cond in conds)
            {
                if (flag == false) break;
                string paraValue = values[cond.SourceId];

                switch (cond.Operation)
                {
                    case ConditionOperation.IsInValue:
                        if (!cond.Values.Split(',').Contains(paraValue))
                            flag = false;
                        break;

                    case ConditionOperation.Default:
                        string[] defConds = cond.Values.Split(',');
                        int paraValInt = int.Parse(paraValue);

                        foreach(string defCond in defConds)
                        {
                            if (!flag) break;

                            if (defCond.StartsWith("<="))
                            {
                                int def = int.Parse(defCond.Substring(2));
                                if (paraValInt <= def) flag = false;
                            }
                            else if (defCond.StartsWith("<"))
                            {
                                int def = int.Parse(defCond.Substring(1));
                                if (paraValInt < def) flag = false;
                            }
                            else if (defCond.StartsWith(">="))
                            {
                                int def = int.Parse(defCond.Substring(2));
                                if (paraValInt >= def) flag = false;
                            }
                            else if (defCond.StartsWith(">"))
                            {
                                int def = int.Parse(defCond.Substring(1));
                                if (paraValInt > def) flag = false;
                            }
                            else
                            {
                                int def = int.Parse(defCond);
                                if (paraValInt == def) flag = false;
                            }
                        }
                        break;

                    case ConditionOperation.NotEqual:
                        if (cond.Values == paraValue)
                            flag = false;
                        break;

                    case ConditionOperation.Equal:
                        if (cond.Values != paraValue)
                            flag = false;
                        break;

                    case ConditionOperation.LowerThan:
                        int valLT = int.Parse(paraValue);
                        int valLTo = int.Parse(cond.Values);
                        if ((valLT < valLTo) == false)
                            flag = false;
                        break;

                    case ConditionOperation.LowerEqualThan:
                        int valLET = int.Parse(paraValue);
                        int valLETo = int.Parse(cond.Values);
                        if ((valLET <= valLETo) == false)
                            flag = false;
                        break;

                    case ConditionOperation.GreatherThan:
                        int valGT = int.Parse(paraValue);
                        int valGTo = int.Parse(cond.Values);
                        if ((valGT > valGTo) == false)
                            flag = false;
                        break;

                    case ConditionOperation.GreatherEqualThan:
                        int valGET = int.Parse(paraValue);
                        int valGETo = int.Parse(cond.Values);
                        if ((valGET >= valGETo) == false)
                            flag = false;
                        break;
                }
            }

            return flag;
        }
    }
}