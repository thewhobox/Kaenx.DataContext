using Kaenx.DataContext.Catalog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kaenx.DataContext.Export
{
    public class DevicesExport
    {
        public List<CatalogViewModel> Catalog = new List<CatalogViewModel>();
        public List<ApplicationViewModel> Apps = new List<ApplicationViewModel>();
        public List<DeviceViewModel> Devices = new List<DeviceViewModel>();
        public List<Hardware2AppModel> Hard2App = new List<Hardware2AppModel>();
        public List<AppParameter> Parameters = new List<AppParameter>();
        public List<AppComObject> ComObjects = new List<AppComObject>();
        public List<AppParameterTypeViewModel> ParamTypes = new List<AppParameterTypeViewModel>();
        public List<AppParameterTypeEnumViewModel> Enums = new List<AppParameterTypeEnumViewModel>();
    }
}
