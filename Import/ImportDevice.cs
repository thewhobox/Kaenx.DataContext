using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Kaenx.DataContext.Import
{
    public class ImportDevice : INotifyPropertyChanged
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ApplicationName { get; set; }
        public string ImageUrl { get; set; }
        public string Description { get; set; }
        public string Additional1 { get; set; }
        public string Additional2 { get; set; }
        public bool ExistsInDatabase { get; set; } = false;

        private ImportState _state = ImportState.Waiting;
        public ImportState State
        {
            get { return _state; }
            set { _state = value; Changed("State"); }
        }

        private string _action;
        public string Action
        {
            get { return _action; }
            set { _action = value; Changed("Action"); }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;

        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public enum ImportState
    {
        Waiting,
        Importing,
        Finished,
        Error
    }
}
