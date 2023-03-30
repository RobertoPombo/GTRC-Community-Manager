using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Controls;
using GTRCLeagueManager.Database;

namespace GTRCLeagueManager
{
    public class ResultsLine
    {

        public static string ResultsLinePath = AppDomain.CurrentDomain.BaseDirectory + "data\\resultslines.json";
        public static List<ResultsLine> ResultsLineList = new();

        private int id = -1;

        private int position = -1;
        private int currentdriverid = 0;
        private int entryid = -1;
        private int laps = -1;
        private int time = 0;
        private int[] timegapleader = new int[2] { 0, 0 };
        private int[] timegapintervall = new int[2] { 0, 0 };
        private int bestlap = 0;
        private int bestlapgapleader = 0;
        private int bestlapgapintervall = 0;
        private int carid = -1;
        private int ballast = 0;
        private int restrictor = 0;
        private string space = "";
        private Canvas line = new();
        private Canvas trianglelefttop = new();
        private Canvas triangleleftbottom = new();
        private Canvas trianglerighttop = new();
        private Canvas trianglerightbottom = new();
        private Canvas arrowup = new();
        private Canvas arrowdown = new();

        public ResultsLine()
        {
            ResultsLineList.Add(this);
            ResultsLineID = id;
        }

        public int ResultsLineID
        {
            get { return id; }
            set { id = ResultsLineList.IndexOf(this); }
        }

        public static void ReadJson()
        {
            try { JsonConvert.DeserializeObject<ResultsLine[]>(File.ReadAllText(ResultsLinePath, Encoding.Unicode)); }
            catch { return; }
        }

        public static List<string> ReturnPropsAsList()
        {
            List<string> list = new();
            List<string> blackListProperties = new List<string> { "EntryID", "CurrentDriverID",  "CarID" };
            foreach (PropertyInfo property in typeof(ResultsLine).GetProperties()) { if (!blackListProperties.Contains(property.Name)) { list.Add(property.Name); } }
            //foreach (string key in Entry.ReturnPropsAsList()) { list.Add("Entry " + key); }
            //foreach (string key in Driver.ReturnPropsAsList()) { list.Add("CurrentDriver " + key); }
            //foreach (string key in Car.ReturnPropsAsList()) { list.Add("OnServer Car " + key); }
            return list;
        }

        public Dictionary<string, dynamic> ReturnAsDict()
        {
            Dictionary<string, dynamic> dict = new();
            List<string> blackListProperties = new() { "CurrentDriverID", "EntryID", "CarID" };
            foreach (PropertyInfo property in typeof(ResultsLine).GetProperties())
            {
                if (!blackListProperties.Contains(property.Name))
                {
                    if (property.GetValue(this) != null) { dict[property.Name] = property.GetValue(this); }
                    else { dict[property.Name] = ""; }
                }
            }/*
            foreach (string key in Entry.Statics.GetByID(entryid).ReturnAsDict().Keys)
            {
                dict["Entry " + key] = Entry.Statics.GetByID(entryid).ReturnAsDict()[key];
            }
            foreach (string key in Driver.Statics.GetByUniqProp(currentdriverid).ReturnAsDict().Keys)
            {
                dict["CurrentDriver " + key] = Driver.Statics.GetByUniqProp(currentdriverid).ReturnAsDict()[key];
            }
            foreach (string key in Car.Statics.GetByUniqProp(carid).ReturnAsDict().Keys)
            {
                dict["OnServer Car " + key] = Car.Statics.GetByUniqProp(carid).ReturnAsDict()[key];
            }*/
            return dict;
        }

        public int Position
        {
            get { return position; }
            set { if (value > 0) { position = value; } }
        }

        public int CurrentDriverID
        {
            get { return currentdriverid; }
            set { currentdriverid = value; }
        }

        public int EntryID
        {
            get { return entryid; }
            set { entryid = value; }
        }

        public int Laps
        {
            get { return laps; }
            set
            {
                if (value >= 0) { laps = value; }
                if (id > 0)
                {
                    int laps0 = ResultsLineList[0].laps;
                    int laps1 = ResultsLineList[id - 1].laps;
                    timegapleader[0] = laps0 - laps;
                    timegapintervall[0] = laps1 - laps;
                }
            }
        }

        public string Time
        {
            get { if (time != 0) { return Basics.Ms2Laptime(time); } else { return ""; } }
            set
            {
                if (value != null && value != Int32.MaxValue.ToString() && value != Int32.MinValue.ToString()) { Int32.TryParse(value, out time); }
                if (id > 0)
                {
                    int time0 = ResultsLineList[0].time;
                    int time1 = ResultsLineList[id - 1].time;
                    if (time0 != 0) { timegapleader[1] = time - time0; }
                    if (time1 != 0) { timegapintervall[1] = time - time1; }
                }
            }
        }

        public string TimeGapLeader
        {
            get
            {
                if (timegapleader[0] > 0) { return "+" + timegapleader[0].ToString() + 'L'; }
                else if (timegapleader[0] == 0 && timegapleader[1] > 0) { return "+" + Basics.Ms2Laptime(timegapleader[1]); }
                else { return ""; }
            }
        }
        public string TimeGapIntervall
        {
            get
            {
                if (timegapintervall[0] > 0) { return "+" + timegapintervall[0].ToString() + 'L'; }
                else if (timegapintervall[0] == 0 && timegapintervall[1] > 0) { return "+" + Basics.Ms2Laptime(timegapintervall[1]); }
                else { return ""; }
            }
        }

        public string BestLap
        {
            get { if (bestlap != 0) { return Basics.Ms2Laptime(bestlap); } else { return ""; } }
            set
            {
                if (value != null && value != Int32.MaxValue.ToString() && value != Int32.MinValue.ToString()) { Int32.TryParse(value, out bestlap); }
                if (id > 0)
                {
                    int bestlap0 = ResultsLineList[0].bestlap;
                    int bestlap1 = ResultsLineList[id - 1].bestlap;
                    if (bestlap0 != 0) { bestlapgapleader = bestlap - bestlap0; }
                    if (bestlap1 != 0) { bestlapgapintervall = bestlap - bestlap1; }
                }
            }
        }

        public string BestLapGapLeader
        {
            get
            {
                if (bestlapgapleader > 0) { return "+" + Basics.Ms2Laptime(bestlapgapleader); }
                else { return ""; }
            }
        }

        public string BestLapGapIntervall
        {
            get
            {
                if (bestlapgapintervall > 0) { return "+" + Basics.Ms2Laptime(bestlapgapintervall); }
                else { return ""; }
            }
        }

        public int CarID
        {
            get { return carid; }
            set { if (value >= 0 && value < Car.Statics.List.Count) { carid = value; } }
        }

        public int Ballast
        {
            get { return ballast; }
            set { if (value >= 0 && value <= 30) { ballast = value; } }
        }

        public int Restrictor
        {
            get { return restrictor; }
            set { if (value >= 0 && value <= 100) { restrictor = value; } }
        }

        public string Space { get { return space; } }

        public Canvas Line
        {
            get { return line; }
            set { line = value; }
        }

        public Canvas TriangleLeftTop
        {
            get { return trianglelefttop; }
            set { trianglelefttop = value; }
        }

        public Canvas TriangleLeftBottom
        {
            get { return triangleleftbottom; }
            set { triangleleftbottom = value; }
        }

        public Canvas TriangleRightTop
        {
            get { return trianglerighttop; }
            set { trianglerighttop = value; }
        }

        public Canvas TriangleRightBottom
        {
            get { return trianglerightbottom; }
            set { trianglerightbottom = value; }
        }

        public Canvas ArrowUp
        {
            get { return arrowup; }
            set { arrowup = value; }
        }

        public Canvas ArrowDown
        {
            get { return arrowdown; }
            set { arrowdown = value; }
        }
    }
}
