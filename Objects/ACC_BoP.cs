using Newtonsoft.Json;
using Database;
using Scripts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GTRC_Community_Manager
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

        public void Create(Event _event)
        {
            entries = new List<ACC_BoP_Entry>();
            List<EventsCars> eventsCars = EventsCars.GetAnyBy(nameof(EventsCars.EventID), _event.ID);
            foreach (Track _track in Track.Statics.List)
            {
                foreach (EventsCars eventCar in eventsCars)
                {
                    if (eventCar.Ballast > 0 || eventCar.Restrictor > 0)
                    {
                        ACC_BoP_Entry accBoPEntry = new();
                        entries.Add(accBoPEntry);
                        accBoPEntry.track = _track.AccTrackID;
                        accBoPEntry.carModel = eventCar.ObjCar.AccCarID;
                        accBoPEntry.ballastKg = eventCar.Ballast;
                        accBoPEntry.restrictor = eventCar.Restrictor;
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
