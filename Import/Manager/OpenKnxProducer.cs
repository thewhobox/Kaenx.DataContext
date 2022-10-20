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
    public class OpenKnxProducer : IManager
    {
        private KnxProdFileManager _manager;
        private string _zipPath = "";
        private string manu;

        public OpenKnxProducer(string path) : base(path, 8) { }
        
        public override bool CheckManager()
        {
            if(!File.Exists(_path) || !_path.EndsWith(".xml")) return false;


            XDocument xdoc;
            try{
                xdoc = XDocument.Load(_path);
                
                if(xdoc.Root.Name.LocalName == "KNX")
                {
                    string ns = xdoc.Root.Name.NamespaceName;
                    XElement temp = xdoc.Root.Element(XName.Get("ManufacturerData", ns));
                    temp = temp.Element(XName.Get("Manufacturer", ns));
                    manu = temp.Attribute("RefId").Value;
                    if(temp.Element(XName.Get("Catalog", ns)) != null
                        && temp.Element(XName.Get("ApplicationPrograms", ns)) != null
                        && temp.Element(XName.Get("Hardware", ns)) != null)
                        return true;
                }
            } catch {
                return false;
            }
            return false;
        }

        public override List<string> GetLanguages()
        {
            CreateTempZip();
            return _manager.GetLanguages();
        }

        public override List<ImportDevice> GetDeviceList(CatalogContext context = null)
        {
            CreateTempZip();
            return _manager.GetDeviceList();
        }

        public override void StartImport(List<ImportDevice> ids, CatalogContext context)
        {
            CreateTempZip();
            _manager.StartImport(ids, context);
        }

        public override void Dispose()
        {
            _manager.Dispose();

            string temp = Path.GetTempPath();
            string folder = Path.Combine(temp, "KnxProd");
            Directory.Delete(folder, true);
        }

        private void CreateTempZip()
        {
            if(!string.IsNullOrEmpty(_zipPath)) return;

            string temp = Path.GetTempPath();
            string folder = Path.Combine(temp, "KnxProd");
            string folderFiles = Path.Combine(folder, "ProductDataFile");
            string folderManu = Path.Combine(folderFiles, manu);
            string zipPath = Path.Combine(folder, "out.knxprod");

            if(Directory.Exists(folder))
                Directory.Delete(folder, true);

            Directory.CreateDirectory(folder);
            Directory.CreateDirectory(folderManu);


            XDocument xdoc = XDocument.Load(_path);
            string ns = xdoc.Root.Name.NamespaceName;
            XElement xmanu = xdoc.Root.Element(XName.Get("ManufacturerData", ns)).Element(XName.Get("Manufacturer", ns));

            string manuId = xmanu.Attribute("RefId").Value;
            XElement xcata = xmanu.Element(XName.Get("Catalog", ns));
            XElement xhard = xmanu.Element(XName.Get("Hardware", ns));
            XElement xappl = xmanu.Element(XName.Get("ApplicationPrograms", ns));

            //Save Catalog
            xhard.Remove();
            xappl.Remove();
            xdoc.Save(Path.Combine(folderManu, "Catalog.xml"));

            xcata.Remove();
            xmanu.Add(xhard);
            xdoc.Save(Path.Combine(folderManu, "Hardware.xml"));

            xhard.Remove();
            xmanu.Add(xappl);
            string appId = xappl.Element(XName.Get("ApplicationProgram", ns)).Attribute("Id").Value;
            xdoc.Save(Path.Combine(folderManu, $"{appId}.xml"));



            System.IO.Compression.ZipFile.CreateFromDirectory(folderFiles, zipPath);
                Directory.Delete(folderFiles, true);
            
            _manager = new KnxProdFileManager(zipPath);
        }
    }
}