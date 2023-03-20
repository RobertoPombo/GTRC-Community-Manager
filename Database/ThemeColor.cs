using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Windows.Documents;
using System.Windows.Media;

namespace GTRCLeagueManager.Database
{
    public class ThemeColor : DatabaseObject<ThemeColor>
    {
        [NotMapped][JsonIgnore] public static StaticDbField<ThemeColor> Statics { get ; set; }
        static ThemeColor()
        {
            Statics = new(true)
            {
                Table = "Colors",
                UniquePropertiesNames = new List<List<string>>() { new List<string>() { nameof(Alpha), nameof(Red), nameof(Green), nameof(Blue) } },
                ToStringPropertiesNames = new List<string>() { nameof(Alpha), nameof(Red), nameof(Green), nameof(Blue) },
                PublishList = () => PublishList()
            };
        }
        public ThemeColor() { This = this; Initialize(true, true); }
        public ThemeColor(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public ThemeColor(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        private Int16 alpha = 0;
        private Int16 red = 0;
        private Int16 green = 0;
        private Int16 blue = 0;

        public Int16 Alpha
        {
            get { return alpha; }
            set { alpha = value; if (ReadyForList) { SetNextAvailable(); } }
        }

        public Int16 Red
        {
            get { return red; }
            set { red = value; if (ReadyForList) { SetNextAvailable(); } }
        }

        public Int16 Green
        {
            get { return green; }
            set { green = value; if (ReadyForList) { SetNextAvailable(); } }
        }

        public Int16 Blue
        {
            get { return blue; }
            set { blue = value; if (ReadyForList) { SetNextAvailable(); } }
        }

        [JsonIgnore] public SolidColorBrush Preview
        {
            get { return new SolidColorBrush(Color.FromArgb((byte)Alpha, (byte)Red, (byte)Green, (byte)Blue)); }
        }

        public static void PublishList() { }

        public override void SetNextAvailable()
        {
            bool breakLoop = false; alpha--; red--; green--; blue--;
            Int16 _alpha0 = alpha; Int16 _red0 = red; Int16 _green0 = green; Int16 _blue0 = blue;
            for (int nr1 = -1; nr1 < 256; nr1++)
            {
                alpha++; if (alpha > 255 || alpha < 0) { alpha = 0; }
                for (int nr2 = -1; nr2 < 256; nr2++)
                {
                    red++; if (red > 255 || red < 0) { red = 0; }
                    for (int nr3 = -1; nr3 < 256; nr3++)
                    {
                        green++; if (green > 255 || green < 0) { green = 0; }
                        for (int nr4 = -1; nr4 < 256; nr4++)
                        {
                            blue++; if (blue > 255 || blue < 0) { blue = 0; }
                            if (IsUnique() || (_alpha0 == alpha && _red0 == red && _green0 == green && _blue0 == blue)) { breakLoop = true; break; }
                        }
                        if (breakLoop) { break; }
                    }
                    if (breakLoop) { break; }
                }
                if (breakLoop) { break; }
            }
        }
    }
}
