using System;
using System.Collections.Generic;

namespace GTRCLeagueManager
{
    public class MainVM : ObservableObject
    {
        public static List<MainVM> List = new List<MainVM>();
        private string logcurrenttext = " ";

        public MainVM() { List.Add(this); }

        public string LogCurrentText
        {
            get { return logcurrenttext; }
            set { logcurrenttext = value; RaisePropertyChanged(); }
        }
    }
}
