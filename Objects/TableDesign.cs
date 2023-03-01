using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using GTRCLeagueManager.Database;

namespace GTRCLeagueManager
{
    public class TableDesign : ObservableObject
    {

        public static string TableDesignPath = AppDomain.CurrentDomain.BaseDirectory + "data\\design.json";

        private string columnname = "Standard";
        private string backgroundcolor = "{StaticResource color1}";
        private string fontcolor = "{StaticResource color2}";
        private string fontfamily = "Metropolis";
        private int fontsize = 13;
        private FontWeight fontweight = FontWeight.FromOpenTypeWeight(500);
        private Thickness padding = new Thickness(0, 0, 0, 0);
        private string deftext = MainWindow.DefText;

        public TableDesign() { }

        public static List<string> ReturnPropsAsList()
        {
            List<string> list = new List<string>();
            List<string> blackListProperties = new List<string> { "ColumnNameList" };
            foreach (PropertyInfo property in typeof(TableDesign).GetProperties()) { if (!blackListProperties.Contains(property.Name)) { list.Add(property.Name); } }
            return list;
        }

        public Dictionary<string, dynamic> ReturnAsDict()
        {
            Dictionary<string, dynamic> dict = new Dictionary<string, dynamic>();
            List<string> blackListProperties = new List<string> { "ColumnNameList" };
            foreach (PropertyInfo property in typeof(TableDesign).GetProperties()) { if (!blackListProperties.Contains(property.Name)) { dict[property.Name] = property.GetValue(this); } }
            return dict;
        }

        public static List<string> ColumnNameList
        {
            get { return ResultsLine.ReturnPropsAsList(); }
            set {}
        }

        public string ColumnName
        {
            get { return columnname; }
            set { if (ResultsLine.ReturnPropsAsList().Contains(value)) { columnname = value; this.RaisePropertyChanged(); } }
        }

        public string BackgroundColor
        {
            get { return backgroundcolor; }
            set { backgroundcolor = value; this.RaisePropertyChanged(); }
        }

        public string FontColor
        {
            get { return fontcolor; }
            set { fontcolor = value; this.RaisePropertyChanged(); }
        }

        public string FontFamily
        {
            get { return fontfamily; }
            set { fontfamily = value; }
        }

        public int FontSize
        {
            get { return fontsize; }
            set { if (value > 0) { fontsize = value; this.RaisePropertyChanged(); } }
        }

        public int FontThickness
        {
            get { return fontweight.ToOpenTypeWeight(); }
            set { if (value >= 1 && value <= 999) { fontweight = FontWeight.FromOpenTypeWeight(value); this.RaisePropertyChanged(); } }
        }

        public double Padding
        {
            get { return padding.Left; }
            set { padding = new Thickness(value); this.RaisePropertyChanged(); }
        }

        public string DefText
        {
            get { return deftext; }
            set { deftext = value; this.RaisePropertyChanged(); }
        }
    }
}
