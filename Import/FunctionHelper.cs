using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Text;
using Kaenx.DataContext.Import.Dynamic;
using Kaenx.DataContext.Import.Values;

namespace Kaenx.DataContext.Import {

    public class FunctionHelper {

        public static byte[] ObjectToByteArray(object obj, bool compress = false, string ns = null)
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

            if(compress)
            {
                byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(text);
                using (var outputStream = new MemoryStream())
                {
                    using (var gZipStream = new GZipStream(outputStream, CompressionMode.Compress))
                        gZipStream.Write(inputBytes, 0, inputBytes.Length);

                    return outputStream.ToArray();
                }


                //MemoryStream ms = new MemoryStream();
                //GZipStream gzip = new GZipStream(ms, CompressionLevel.Fastest);
                //StreamWriter sw = new StreamWriter(gzip);
                //sw.Write(text);
                //gzip.Dispose();
                //return ms.ToArray();
            }

            return System.Text.Encoding.UTF8.GetBytes(text);
        }

        public static T ByteArrayToObject<T>(byte[] obj, bool compress = false, string ns = null)
        {
            string text;

            if (compress)
            {
                MemoryStream ms = new MemoryStream(obj);
                GZipStream gzip = new GZipStream(ms, CompressionMode.Decompress);
                StreamReader sr = new StreamReader(gzip);
                text = sr.ReadToEnd();
                gzip.Dispose();
            }
            else
            {
                text = System.Text.Encoding.UTF8.GetString(obj);
            }
            

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

        public static bool CheckConditions(List<ParamCondition> conds, Dictionary<long, IValues> values)
        {
            bool flag = true;
            if(conds == null) return true;

            foreach (ParamCondition cond in conds)
            {
                if (flag == false) break;
                string paraValue = values[cond.SourceId].Value;
                if(paraValue == "x") return false;

                switch (cond.Operation)
                {
                    case ConditionOperation.IsInValue:
                        if (!cond.Values.Split(',').Contains(paraValue))
                            flag = false;
                        break;

                    case ConditionOperation.Default:
                        if(!string.IsNullOrEmpty(cond.Values))
                        {
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