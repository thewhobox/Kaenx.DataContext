using Kaenx.DataContext.Import.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Kaenx.DataContext.Import
{
    public class ImportManager
    {
        public static IManager GetImportManager(string filePath)
        {
            IManager manager = null;
            var q = from t in Assembly.GetExecutingAssembly().GetTypes()
                   where t.BaseType == typeof(IManager) && t.Namespace == "Kaenx.DataContext.Import.Manager"
                    select t;

            foreach (Type t in q.ToList())
            {
                IManager _manager = (IManager)Activator.CreateInstance(t, filePath);
                if (_manager.CheckManager())
                {
                    manager = _manager;
                    break;
                }
            }

            return manager;
        }
    }
}
