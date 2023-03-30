using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Windows.Media;
using System.Windows;

namespace GTRCLeagueManager
{
    public static class Basics
    {
        public static readonly int NoID = -1;
        public static readonly Brush StateOff = (Brush)Application.Current.FindResource("color1");
        public static readonly Brush StateOn = (Brush)Application.Current.FindResource("color3");
        public static readonly Brush StateWait = (Brush)Application.Current.FindResource("color6");
        public static readonly Brush StateRun = (Brush)Application.Current.FindResource("color5");
        public static readonly Brush StateRunWait = (Brush)Application.Current.FindResource("color4");

        public static dynamic CastValue(PropertyInfo property, dynamic Value)
        {
            if (Value == null) { Value = ""; }
            switch (property.PropertyType.ToString())
            {
                case "System.Boolean": if (Boolean.TryParse(Value.ToString(), out bool _bool)) { return _bool; } else { return false; }
                case "System.String": return Value.ToString();
                case "System.Int16": if (Int16.TryParse(Value.ToString(), out Int16 _Int16)) { return _Int16; } else { return (Int16)0; }
                case "System.Int32": if (Int32.TryParse(Value.ToString(), out Int32 _Int32)) { return _Int32; } else { return (Int16)0; }
                case "System.Int64": if (Int64.TryParse(Value.ToString(), out Int64 _Int64)) { return _Int64; } else { return (Int64)0; }
                case "System.UInt16": if (UInt16.TryParse(Value.ToString(), out UInt16 _UInt16)) { return _UInt16; } else { return (Int16)0; }
                case "System.UInt32": if (UInt32.TryParse(Value.ToString(), out UInt32 _UInt32)) { return _UInt32; } else { return (UInt32)0; }
                case "System.UInt64": if (UInt64.TryParse(Value.ToString(), out UInt64 _UInt64)) { return _UInt64; } else { return (UInt64)0; }
                case "System.Single": if (Single.TryParse(Value.ToString(), out float _float)) { return _float; } else { return (float)0; }
                case "System.Double": if (Double.TryParse(Value.ToString(), out double _double)) { return _double; } else { return (double)0; }
                case "System.Decimal": if (Decimal.TryParse(Value.ToString(), out decimal _decimal)) { return _decimal; } else { return (decimal)0; }
                case "System.DateTime": if (DateTime.TryParse(Value.ToString(), out DateTime _DateTime)) { return _DateTime; } else { return DateTime.MinValue; }
                case "System.Object": if (Int32.TryParse(Value.ToString(), out Int32 _ID)) { return _ID; } else { return NoID; }
                default: return null;
            }
        }

        public static dynamic GetCastedValue(object obj, PropertyInfo property)
        {
            switch (property.PropertyType.ToString())
            {
                case "System.Boolean": return (bool)property.GetValue(obj);
                case "System.String": return (string)property.GetValue(obj);
                case "System.Int16": return (Int16)property.GetValue(obj);
                case "System.Int32": return (Int32)property.GetValue(obj);
                case "System.Int64": return (Int64)property.GetValue(obj);
                case "System.UInt16": return (UInt16)property.GetValue(obj);
                case "System.UInt32": return (UInt32)property.GetValue(obj);
                case "System.UInt64": return (UInt64)property.GetValue(obj);
                case "System.Single": return (float)property.GetValue(obj);
                case "System.Double": return (double)property.GetValue(obj);
                case "System.Decimal": return (decimal)property.GetValue(obj);
                case "System.DateTime": return (DateTime)property.GetValue(obj);
                case "System.Object": return (int)property.GetValue(obj).GetType().GetProperty("ID").GetValue(property.GetValue(obj));
                default: return property.GetValue(obj);
            }
        }

        public static string RemoveSpaceStartEnd(string s)
        {
            s = s ?? "";
            while (s.Length > 0 && s.Substring(0, 1) == " ") { s = s.Substring(1, s.Length - 1); }
            while (s.Length > 0 && s.Substring(s.Length - 1, 1) == " ") { s = s.Substring(0, s.Length - 1); }
            return s;
        }

        public static string SubStr(string _string, int start)
        {
            return SubStr(_string, start, int.MaxValue);
        }

        public static string SubStr(string _string, int start, int length)
        {
            int ulength = Math.Abs(length);
            if (start < 0)
            {
                start = _string.Length + start;
                start = Math.Max(0, start);
            }
            start = Math.Min(_string.Length, Math.Max(0, start));
            ulength = Math.Min(ulength, _string.Length - start);
            _string = _string.Substring(start, ulength);
            if (length < 0)
            {
                string revString = "";
                foreach (char _char in _string.Reverse()) { revString += _char; }
                return revString;
            }
            else { return _string; }
        }

        public static string StrRemoveSpecialLetters(string str)
        {
            str = str.Replace("ß", "ss");
            str = str.Replace("Ä", "AE"); str = str.Replace("ä", "ae");
            str = str.Replace("Ö", "OE"); str = str.Replace("ö", "oe");
            str = str.Replace("Ü", "UE"); str = str.Replace("ü", "ue");
            str = str.Replace("Á", "A"); str = str.Replace("á", "a");
            str = str.Replace("É", "E"); str = str.Replace("é", "e");
            str = str.Replace("Í", "I"); str = str.Replace("í", "i");
            str = str.Replace("Ó", "O"); str = str.Replace("ó", "o");
            str = str.Replace("Ú", "U"); str = str.Replace("ú", "u");
            str = str.Replace("À", "A"); str = str.Replace("à", "a");
            str = str.Replace("È", "E"); str = str.Replace("è", "e");
            str = str.Replace("Ì", "I"); str = str.Replace("ì", "i");
            str = str.Replace("Ò", "O"); str = str.Replace("ò", "o");
            str = str.Replace("Ù", "U"); str = str.Replace("ù", "u");
            str = str.Replace("Ñ", "N"); str = str.Replace("ñ", "n");
            return str;
        }

        public static string StrRemoveVocals(string str)
        {
            List<string> vocals = new() { "a", "e", "i", "o", "u" };
            foreach (string vocal in vocals)
            {
                str = str.Replace(vocal.ToLower(), "");
                str = str.Replace(vocal.ToUpper(), "");
            }
            return str;
        }

        public static string Ms2Laptime(int ms)
        {
            if (ms == int.MinValue) { ms = int.MaxValue; }
            float flo_input = (float)Math.Abs(ms);
            flo_input = flo_input / 1000;
            int int_std = Convert.ToInt32(Math.Floor(flo_input / 3600));
            int int_min = Convert.ToInt32(Math.Floor((flo_input / 60) - 60 * int_std));
            int int_sek = Convert.ToInt32(Math.Floor(flo_input - 60 * (int_min + 60 * int_std)));
            int int_ms = Convert.ToInt32(Math.Round((flo_input - int_sek - 60 * (int_min + 60 * int_std)) * 1000));
            string str_std, str_min, str_sek, str_ms;

            if (int_ms > 99) { str_ms = int_ms.ToString(); }
            else if (int_ms > 9) { str_ms = "0" + int_ms.ToString(); }
            else { str_ms = "00" + int_ms.ToString(); }

            str_sek = int_sek.ToString() + ".";
            if (int_sek < 10 && (int_min > 0 || int_std > 0)) { str_sek = "0" + str_sek; }

            if (int_min == 0 && int_std == 0) { str_min = ""; }
            else
            {
                str_min = int_min.ToString() + ":";
                if (int_min < 10 && int_std > 0) { str_min = "0" + str_min; }
            }

            if (int_std == 0) { str_std = ""; }
            else { str_std = int_std.ToString() + ":"; }

            return str_std + str_min + str_sek + str_ms;
        }

        public static int Laptime2Ms(string laptime)
        {
            int ms = 0;
            string msStr = "0";
            string secStr = "0";
            string minStr = "0";
            string hStr = "0";
            string[] inputArray = laptime.Split(':');
            List<string> inputList1 = new();
            foreach (string input in inputArray) { inputList1.Add(input); }
            if (inputList1.Count > 2)
            {
                hStr = inputList1[^3];
                if (hStr.Length == 0) { hStr = "0"; }
            }
            if (inputList1.Count > 1)
            {
                minStr = inputList1[^2];
                if (minStr.Length == 0) { minStr = "0"; }
            }
            if (inputList1.Count > 0)
            {
                string[] inputSecArray = inputList1[^1].Split('.', ',');
                List<string> inputList2 = new();
                foreach (string input in inputSecArray) { inputList2.Add(input); }
                if (inputList2.Count == 1)
                {
                    secStr = inputList2[^1];
                    if (secStr.Length == 0) { secStr = "0"; }
                }
                else if (inputList2.Count > 1)
                {
                    secStr = inputList2[^2];
                    if (secStr.Length == 0) { secStr = "0"; }
                    msStr = inputList2[^1];
                    if (msStr.Length == 0) { msStr = "0"; }
                    else if (msStr.Length == 1) { msStr += "00"; }
                    else if (msStr.Length == 2) { msStr += "0"; }
                }
            }
            if (int.TryParse(msStr, out int msInt) && int.TryParse(secStr, out int secInt) && int.TryParse(minStr, out int minInt) && int.TryParse(hStr, out int hInt))
            {
                ms = msInt + 1000 * (secInt + 60 * (minInt + 60 * hInt));
            }
            return ms;
        }

        public static string ValidatedPath(string path0, string path)
        {

            string pathStart;
            string pathName;
            List<char> BlacklistPathChar = new() { '/', ':', '*', '?', '"', '<', '>', '|' };

            if (path == null) { path = path0; }

            if (path.Length < 3 || path.Substring(1, 2) != ":\\")
            {
                pathStart = "//";
                pathName = path;
            }
            else
            {
                pathStart = path[..3];
                pathName = path[pathStart.Length..];
                if (!Directory.Exists(pathStart))
                {
                    pathStart = "//";
                }
            }

            foreach (char pathChar in BlacklistPathChar)
            {
                while (pathName.Contains(pathChar))
                {
                    pathName = pathName.Remove(pathName.IndexOf(pathChar), 1);
                }
            }

            if (pathName.Length > 0 && pathName.Substring(pathName.Length - 1) != "\\")
            {
                pathName += "\\";
            }

            while (pathName.Length > 0 && pathName.Substring(0, 1) == "\\")
            {
                pathName = pathName[1..];
            }

            while (pathName.Contains("\\\\"))
            {
                pathName = pathName.Remove(pathName.IndexOf("\\\\"), 1);
            }

            path = pathStart + pathName;

            if (path.Length >= path0.Length && path.Substring(0, path0.Length) == path0)
            {
                path = "//" + path[path0.Length..];
            }

            return path;
        }

        public static string RelativePath2AbsolutePath(string path0, string path)
        {
            if (path.Length > 0 && path[..2] == "//")
            {
                path = path0 + path[2..];
            }
            return path;
        }

        public static bool PathIsParentOf(string path0, string path1, string path2)
        {
            path1 = RelativePath2AbsolutePath(path0, path1);
            path2 = RelativePath2AbsolutePath(path0, path2);
            if (path2.Length >= path1.Length && path1 == path2.Substring(0, path1.Length)) { return true; }
            else { return false; }
        }

        public static string Date2String(DateTime _date, string parseType)
        {
            int secondint = _date.Second;
            int minuteint = _date.Minute;
            int hourint = _date.Hour;
            int dayint = _date.Day;
            int monthint = _date.Month;
            int yearint = _date.Year;
            string secondstr = secondint.ToString();
            string minutestr = minuteint.ToString();
            string hourstr = hourint.ToString();
            string daystr = dayint.ToString();
            string monthstr = monthint.ToString();
            string yearstr = yearint.ToString();
            if (secondint < 10) { secondstr = "0" + secondstr; }
            if (minuteint < 10) { minutestr = "0" + minutestr; }
            if (hourint < 10) { hourstr = "0" + hourstr; }
            if (dayint < 10) { daystr = "0" + daystr; }
            if (monthint < 10) { monthstr = "0" + monthstr; }
            if (yearint < 10) { yearstr = "0" + yearstr; }
            if (yearint < 100) { yearstr = "0" + yearstr; }
            if (yearint < 1000) { yearstr = "0" + yearstr; }
            int seconddigitcount = 0;
            int minutedigitcount = 0;
            int hourdigitcount = 0;
            int daydigitcount = 0;
            int monthdigitcount = 0;
            int yeardigitcount = 0;
            string text = "";
            foreach (char currentChar in parseType.Reverse())
            {
                switch (currentChar)
                {
                    case 's': seconddigitcount++; if (seconddigitcount <= secondstr.Length) { text = secondstr.Substring(secondstr.Length - seconddigitcount, 1) + text; } break;
                    case 'm': minutedigitcount++; if (minutedigitcount <= minutestr.Length) { text = minutestr.Substring(minutestr.Length - minutedigitcount, 1) + text; } break;
                    case 'h': hourdigitcount++; if (hourdigitcount <= hourstr.Length) { text = hourstr.Substring(hourstr.Length - hourdigitcount, 1) + text; } break;
                    case 'D': daydigitcount++; if (daydigitcount <= daystr.Length) { text = daystr.Substring(daystr.Length - daydigitcount, 1) + text; } break;
                    case 'M': monthdigitcount++; if (monthdigitcount <= monthstr.Length) { text = monthstr.Substring(monthstr.Length - monthdigitcount, 1) + text; } break;
                    case 'Y': yeardigitcount++; if (yeardigitcount <= yearstr.Length) { text = yearstr.Substring(yearstr.Length - yeardigitcount, 1) + text; } break;
                    default: text = currentChar + text; break;
                }
            }
            return text;
        }

        public static string Ms2String(int ms, string parseType)
        {
            if (ms == int.MinValue) { ms = int.MaxValue; }
            float flo_input = Math.Abs(ms);
            flo_input /= 1000;
            int hInt = Convert.ToInt32(Math.Floor(flo_input / 3600));
            int minInt = Convert.ToInt32(Math.Floor((flo_input / 60) - 60 * hInt));
            int secInt = Convert.ToInt32(Math.Floor(flo_input - 60 * (minInt + 60 * hInt)));
            int msInt = Convert.ToInt32(Math.Round((flo_input - secInt - 60 * (minInt + 60 * hInt)) * 1000));
            string msStr = msInt.ToString();
            string secStr = secInt.ToString();
            string minStr = minInt.ToString();
            string hStr = hInt.ToString();
            if (msInt < 10) { msStr = "0" + msStr; }
            if (msInt < 100) { msStr = "0" + msStr; }
            if (secInt < 10) { secStr = "0" + secStr; }
            if (minInt < 10) { minStr = "0" + minStr; }
            if (hInt < 10) { hStr = "0" + hStr; }
            int msDigitCount = 0;
            int secDigitCount = 0;
            int minDigitCount = 0;
            int hDigitCount = 0;
            string text = "";
            foreach (char currentChar in parseType.Reverse())
            {
                switch (currentChar)
                {
                    case 'x': msDigitCount++; if (msDigitCount <= msStr.Length) { text = msStr.Substring(msDigitCount - 1, 1) + text; } break;
                    case 's': secDigitCount++; if (secDigitCount <= secStr.Length) { text = secStr.Substring(secStr.Length - secDigitCount, 1) + text; } break;
                    case 'm': minDigitCount++; if (minDigitCount <= minStr.Length) { text = minStr.Substring(minStr.Length - minDigitCount, 1) + text; } break;
                    case 'h': hDigitCount++; if (hDigitCount <= hStr.Length) { text = hStr.Substring(hStr.Length - hDigitCount, 1) + text; } break;
                    default: text = currentChar + text; break;
                }
            }
            return text;
        }
    }
}
