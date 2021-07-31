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
    public class KnxProdFileManager : IManager, IDisposable
    {
        private ZipArchive Archive { get; set; }



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
                    TranslateXml(catXML, _lanugage);
                    string ns = catXML.Name.NamespaceName;

                    IEnumerable<XElement> catalogItems = catXML.Descendants(XName.Get("CatalogItem", ns));
                    catalogItems = catalogItems.OrderBy(c => c.Attribute("Name").Value);


                    foreach(XElement item in catalogItems)
                    {
                        ImportDevice device = new ImportDevice()
                        {
                            Id = item.Attribute("Id").Value,
                            Name = item.Attribute("Name").Value,
                            Description = item.Attribute("VisibleDescription")?.Value.Replace(Environment.NewLine, " ")
                        };
                        devices.Add(device);
                    }
                }
            }

            return devices;
        }

        public override void StartImport(List<string> id, CatalogContext context)
        {

        }

        private void TranslateXml(XElement xml, string selectedLang)
        {
            if (selectedLang == null) return;



            if (!xml.Descendants(XName.Get("Language", xml.Name.NamespaceName)).Any(l => l.Attribute("Identifier").Value == selectedLang))
            {
                return;
            }


            Dictionary<string, Dictionary<string, string>> transl = new Dictionary<string, Dictionary<string, string>>();


            XElement lang = xml.Descendants(XName.Get("Language", xml.Name.NamespaceName)).Single(l => l.Attribute("Identifier").Value == selectedLang);
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

        public override void Dispose()
        {
            Archive.Dispose();
        }
    }
}
