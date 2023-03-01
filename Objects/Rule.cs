using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Text;
using GTRCLeagueManager.Database;

namespace GTRCLeagueManager
{
    public class Rule : ObservableObject
    {
        public static string RulePath = AppDomain.CurrentDomain.BaseDirectory + "data\\rules.json";

        private string name = "Standard";
        private ObservableCollection<TableDesign> tableDesignList = new ObservableCollection<TableDesign>();
        private TableDesign selectedDesign = null;

        public Rule()
        {
            Name = name;
            TableDesignList.Add(new TableDesign());
            SelectedDesign = TableDesignList[0];
            this.AddDesign = new UICmd((o) => TableDesignList.Add(new TableDesign() { ColumnName = ResultsLine.ReturnPropsAsList()[0] } ));
            this.DelDesign = new UICmd((o) => TableDesignList.Remove((TableDesign)o));
            this.SelDesign = new UICmd((o) => { SelectedDesign = (TableDesign)o; Console.WriteLine(SelectedDesign.DefText); Console.WriteLine(SelectedDesign.FontFamily); });
        }

        public static void ReadJson()
        {
            try { JsonConvert.DeserializeObject<Rule[]>(File.ReadAllText(RulePath, Encoding.Unicode)); }
            catch { return; }
        }

        public static void WriteJson()
        {
            string text = JsonConvert.SerializeObject(RuleVM.ruleList, Formatting.Indented);
            File.WriteAllText(RulePath, text, Encoding.Unicode);
        }

        public Dictionary<string, dynamic> ReturnAsDict()
        {
            Dictionary<string, dynamic> dict = new Dictionary<string, dynamic>();
            foreach (PropertyInfo property in typeof(Car).GetProperties())
            {
                if (property.GetValue(this) != null) { dict[property.Name] = property.GetValue(this); }
                else { dict[property.Name] = ""; }
            }
            return dict;
        }

        public string Name
        {
            get { return name; }
            set
            {
                bool exists = true; int nr = 1; string _name = value; string defName = _name;
                if (RuleVM.ruleList != null)
                {
                    while (exists)
                    {
                        exists = false;
                        foreach (Rule _rule in RuleVM.ruleList) { if (_name == _rule.Name) { exists = true; break; } }
                        if (exists) { _name = defName + "_" + nr.ToString(); nr++; }
                    }
                }
                name = _name;
            }
        }

        public ObservableCollection<TableDesign> TableDesignList
        {
            get { return tableDesignList; }
            set
            {
                if (tableDesignList != value)
                {
                    tableDesignList = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        public TableDesign SelectedDesign
        {
            get { return selectedDesign; }
            set { selectedDesign = value; this.RaisePropertyChanged(); }
        }

        public UICmd AddDesign { get; set; }
        public UICmd DelDesign { get; set; }
        public UICmd SelDesign { get; set; }
    }
}
