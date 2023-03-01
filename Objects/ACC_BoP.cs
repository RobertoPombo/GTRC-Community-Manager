using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GTRCLeagueManager.Database
{
    public class ACC_BoP_Entry
    {
        public string track { get; set; }
        public int carModel { get; set; }
        public int ballastKg { get; set; }
        public int restrictor { get; set; }
    }

    public class ACC_BoP
    {
        public List<ACC_BoP_Entry> entries { get; set; }

        public void WriteJson(string Path)
        {
            string relativePath = Basics.ValidatedPath(MainWindow.currentDirectory, Path);
            string absolutePath = Basics.RelativePath2AbsolutePath(MainWindow.currentDirectory, relativePath);
            if (!Directory.Exists(absolutePath)) { Directory.CreateDirectory(absolutePath); }
            absolutePath += "bop.json";
            string text = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(absolutePath, text, Encoding.Unicode);
        }

        public void Create()
        {
            entries = new List<ACC_BoP_Entry>();
            foreach (Track _track in Track.Statics.List)
            {
                foreach (CarBoP _carBoP in CarBoP.List)
                {
                    if (_carBoP.Ballast > 0 || _carBoP.Restrictor > 0)
                    {
                        ACC_BoP_Entry accBoPEntry = new ACC_BoP_Entry();
                        entries.Add(accBoPEntry);
                        accBoPEntry.track = _track.AccTrackID;
                        accBoPEntry.carModel = _carBoP.Car.AccCarID;
                        accBoPEntry.ballastKg = _carBoP.Ballast;
                        accBoPEntry.restrictor = _carBoP.Restrictor;
                    }
                }
            }
        }

        public void CreateEmpty()
        {
            entries = new List<ACC_BoP_Entry>();
        }
    }
}
