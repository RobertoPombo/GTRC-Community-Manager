using Google.Apis.Sheets.v4.Data;
using GTRCLeagueManager.Database;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace GTRCLeagueManager
{
    public class SettingsVM : ObservableObject
    {
        public static SettingsVM Instance;
        private static readonly string DBPathSettings = MainWindow.dataDirectory + "config database.json";
        private static readonly string DisBotPathSettings = MainWindow.dataDirectory + "config discordbot.json";
        private static readonly string GSPathSettings = MainWindow.dataDirectory + "config gsheets.json";
        private static readonly List<string> GSheetNames = new() { "Practice Leaderboard", "Pre-Qualifying Leaderboard", "Balance of Performace", "Stewarding", "Registered Entries", "Final Results", "Entries Current Event", "Car Changes" };

        public SettingsVM()
        {
            Instance = this;
            this.RestoreDBSettingsCmd = new UICmd((o) => RestoreDBSettings());
            this.SaveDBSettingsCmd = new UICmd((o) => SaveDBSettings());
            this.AddDBPresetCmd = new UICmd((o) => AddDBPreset());
            this.DelDBPresetCmd = new UICmd((o) => DelDBPreset());
            this.RestoreDisBotSettingsCmd = new UICmd((o) => RestoreDisBotSettings());
            this.SaveDisBotSettingsCmd = new UICmd((o) => SaveDisBotSettings());
            this.AddDisBotPresetCmd = new UICmd((o) => AddDisBotPreset());
            this.DelDisBotPresetCmd = new UICmd((o) => DelDisBotPreset());
            this.RestoreGSSettingsCmd = new UICmd((o) => RestoreGSSettings());
            this.SaveGSSettingsCmd = new UICmd((o) => SaveGSSettings());
            RestoreDBSettings();
            RestoreDisBotSettings();
            RestoreGSSettings();
        }




        //Database Connection Settings:

        public static readonly ObservableCollection<string> dBConnectionTypes = new() { "Local", "Network" };
        public ObservableCollection<string> DBConnectionTypes { get { return dBConnectionTypes; } }

        private ObservableCollection<DBConnection> dblist = new();
        public ObservableCollection<DBConnection> DBList { get { return dblist; } set { dblist = value; RaisePropertyChanged(); } }

        private DBConnection? activeDBConnection = null;
        public DBConnection? ActiveDBConnection
        {
            get { return activeDBConnection; }
        }

        public void UpdateActiveDBConnection()
        {
            try { SQL.Connection.Close(); } catch { }
            DBConnection? nextActiveDBConnection = null;
            foreach (DBConnection _dbCon in DBList)
            {
                if (_dbCon.IsActive)
                {
                    foreach (DBConnection _dbCon2 in DBList)
                    {
                        if (_dbCon != _dbCon2) { _dbCon2.IsActive = false; }
                    }
                    nextActiveDBConnection = _dbCon;
                    break;
                }
            }
            if (nextActiveDBConnection != activeDBConnection)
            {
                activeDBConnection = nextActiveDBConnection;
                //MainVM.List[0].LogCurrentText = "Trying to connect to database...";
                try
                {
                    _ = SQL.ConVal(ActiveDBConnection);
                    if (SQL.defaultConnectionString != SQL.ConnectionString)
                    {
                        SQL.Connection = new SqlConnection(SQL.ConnectionString);
                        SQL.Connection.Open();
                        MainVM.List[0].LogCurrentText = "Connection to database succeded.";
                        DatabaseVM.Instance.InitializeDatabase();
                    }
                    else { MainVM.List[0].LogCurrentText = "No database connected."; }
                }
                catch { nextActiveDBConnection.IsActive = false; MainVM.List[0].LogCurrentText = "Connection to database failed!"; }
            }
        }

        private DBConnection selectedDBConnection;
        public DBConnection SelectedDBConnection { get { return selectedDBConnection; } set { selectedDBConnection = value; RaisePropertyChanged(); } }

        private bool stateShowHidePassword = false;
        public bool StateShowHidePassword { get { return stateShowHidePassword; } set { stateShowHidePassword = value; } }

        public void RestoreDBSettings()
        {
            try
            {
                DBList.Clear();
                DBList = JsonConvert.DeserializeObject<ObservableCollection<DBConnection>>(File.ReadAllText(DBPathSettings, Encoding.Unicode));
                PublishDBListIDs();
                MainVM.List[0].LogCurrentText = "Database connection settings restored.";
            }
            catch { MainVM.List[0].LogCurrentText = "Restore database connection settings failed!"; }
            if (DBList.Count == 0) { DBList.Add(new DBConnection()); }
            UpdateActiveDBConnection();
            if (ActiveDBConnection == null) { SelectedDBConnection = DBList[0]; } else { SelectedDBConnection = ActiveDBConnection; }
        }

        public void SaveDBSettings()
        {
            PublishDBListIDs();
            string text = JsonConvert.SerializeObject(DBList, Formatting.Indented);
            File.WriteAllText(DBPathSettings, text, Encoding.Unicode);
            MainVM.List[0].LogCurrentText = "Database connection settings saved.";
        }

        public void AddDBPreset()
        {
            DBConnection newCon = new(); DBList.Add(newCon); SelectedDBConnection = newCon;
        }

        public void DelDBPreset()
        {
            if (DBList.Count > 1 && !SelectedDBConnection.IsActive) { DBList.Remove(SelectedDBConnection); SelectedDBConnection = DBList[0]; }
        }

        public void PublishDBListIDs()
        {
            DBConnection.ListIDs.Clear();
            foreach (DBConnection _dbConnection in DBList) { DBConnection.ListIDs.Add(_dbConnection); }
        }

        public UICmd RestoreDBSettingsCmd { get; set; }
        public UICmd SaveDBSettingsCmd { get; set; }
        public UICmd AddDBPresetCmd { get; set; }
        public UICmd DelDBPresetCmd { get; set; }




        //Discord Bot Settings:

        public static readonly ObservableCollection<DiscordBot> discordBotList = new ObservableCollection<DiscordBot>() {
            new DiscordBot() { Name = "Azubi des Monats", Token = "MTAwNDc5NTMxNjQ2MzIyNzA0MA.G4Qg1w.-_7ccWcVoZrun6jx-k_7KreF-1fE-blNNhrJzc", DiscordID = 1004795316463227040 },
            new DiscordBot() { Name = "Mitarbeiter des Monats", Token = "MTAwODQwMDUyMzM5MDYzNjE4NA.GuiMFH.L0A38VZ9n1enIUMCyAn5-HTqVlLl99XzsqFLW0", DiscordID = 1008400523390636184 }
        };
        public ObservableCollection<DiscordBot> DiscordBotList { get { return discordBotList; } }

        private ObservableCollection<DisBotPreset> disBotPresetList = new();
        public ObservableCollection<DisBotPreset> DisBotPresetList { get { return disBotPresetList; } set { disBotPresetList = value; RaisePropertyChanged(); } }

        private DisBotPreset? activeDisBotPreset = null;
        public DisBotPreset? ActiveDisBotPreset
        {
            get { return activeDisBotPreset; }
        }

        public Thread SignInOutBotThread;

        public void UpdateActiveDiscordBot()
        {
            if (SignInOutBot.Instance?._client is not null)
            {
                try { SignInOutBot.Instance.StopBot(); }
                catch { }
            }
            DisBotPreset? nextActiveDisBotPreset = null;
            foreach (DisBotPreset _disBotPre in DisBotPresetList)
            {
                if (_disBotPre.IsActive)
                {
                    foreach (DisBotPreset _disBotPre2 in DisBotPresetList)
                    {
                        if (_disBotPre != _disBotPre2) { _disBotPre2.IsActive = false; }
                    }
                    nextActiveDisBotPreset = _disBotPre;
                    break;
                }
            }
            if (nextActiveDisBotPreset != activeDisBotPreset)
            {
                activeDisBotPreset = nextActiveDisBotPreset;
                if (nextActiveDisBotPreset is not null)
                {
                    //MainVM.List[0].LogCurrentText = "Trying to start discord bot...";
                    try
                    {
                        SignInOutBot signInOutBot = new(nextActiveDisBotPreset);
                        SignInOutBotThread = new(obj => signInOutBot.RunBotAsync().GetAwaiter().GetResult());
                        SignInOutBotThread.Start();
                        MainVM.List[0].LogCurrentText = "Discord bot is running.";
                    }
                    catch { nextActiveDisBotPreset.IsActive = false; MainVM.List[0].LogCurrentText = "Discord bot failed!"; }
                }
                else { MainVM.List[0].LogCurrentText = "Discord bot stopped."; }
            }
        }

        private DisBotPreset selectedDisBotPreset;
        public DisBotPreset SelectedDisBotPreset { get { return selectedDisBotPreset; } set { selectedDisBotPreset = value; RaisePropertyChanged(); } }

        public void RestoreDisBotSettings()
        {
            try
            {
                DisBotPresetList.Clear();
                DisBotPresetList = JsonConvert.DeserializeObject<ObservableCollection<DisBotPreset>>(File.ReadAllText(DisBotPathSettings, Encoding.Unicode));
                PublishDisBotListIDs();
                MainVM.List[0].LogCurrentText = "Discord bot settings restored.";
            }
            catch { MainVM.List[0].LogCurrentText = "Restore discord bot settings failed!"; }
            if (DisBotPresetList.Count == 0) { DisBotPresetList.Add(new DisBotPreset()); }
            UpdateActiveDiscordBot();
            if (ActiveDisBotPreset == null) { SelectedDisBotPreset = DisBotPresetList[0]; } else { SelectedDisBotPreset = ActiveDisBotPreset; }
        }

        public void SaveDisBotSettings()
        {
            PublishDisBotListIDs();
            string text = JsonConvert.SerializeObject(DisBotPresetList, Formatting.Indented);
            File.WriteAllText(DisBotPathSettings, text, Encoding.Unicode);
            MainVM.List[0].LogCurrentText = "Discord bot settings saved.";
        }

        public void AddDisBotPreset()
        {
            DisBotPreset newPreset = new(); DisBotPresetList.Add(newPreset); SelectedDisBotPreset = newPreset;
        }

        public void DelDisBotPreset()
        {
            if (DisBotPresetList.Count > 1 && !SelectedDisBotPreset.IsActive) { DisBotPresetList.Remove(SelectedDisBotPreset); SelectedDisBotPreset = DisBotPresetList[0]; }
        }

        public void PublishDisBotListIDs()
        {
            DisBotPreset.ListIDs.Clear();
            foreach (DisBotPreset _disBotPre in DisBotPresetList) { DisBotPreset.ListIDs.Add(_disBotPre); }
        }

        public UICmd RestoreDisBotSettingsCmd { get; set; }
        public UICmd SaveDisBotSettingsCmd { get; set; }
        public UICmd AddDisBotPresetCmd { get; set; }
        public UICmd DelDisBotPresetCmd { get; set; }





        //Google Sheets Settings:

        private ObservableCollection<GSheet> gslist = new();
        public ObservableCollection<GSheet> GSList { get { return gslist; } set { gslist = value; RaisePropertyChanged(); } }

        public void RestoreGSSettings()
        {
            try
            {
                dynamic? obj = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(GSPathSettings, Encoding.Unicode));
                GSList = new ObservableCollection<GSheet>();
                foreach (string _sheetName in GSheetNames)
                {
                    GSheet _gSheet = new() { Name = _sheetName };   
                    GSList.Add(_gSheet);
                    if (obj is IList)
                    {
                        foreach (var item in obj)
                        {
                            string _name = item.Name ?? "";
                            if (_name == _gSheet.Name)
                            {
                                _gSheet.DocID = item.DocID ?? _gSheet.DocID;
                                _gSheet.SheetID = item.SheetID ?? _gSheet.SheetID;
                                _gSheet.Range = item.Range ?? _gSheet.Range;
                            }
                        }
                    }
                }
                PublishGSheetListIDs();
                MainVM.List[0].LogCurrentText = "G-Sheet settings restored.";
            }
            catch { MainVM.List[0].LogCurrentText = "Restore g-sheet settings failed!"; }
        }

        public void SaveGSSettings()
        {
            PublishGSheetListIDs();
            string text = JsonConvert.SerializeObject(GSList, Formatting.Indented);
            File.WriteAllText(GSPathSettings, text, Encoding.Unicode);
            MainVM.List[0].LogCurrentText = "Entries settings saved.";
        }

        public void PublishGSheetListIDs()
        {
            GSheet.ListIDs.Clear();
            foreach (GSheet _gSheet in GSList) { GSheet.ListIDs.Add(_gSheet); }
        }

        public UICmd RestoreGSSettingsCmd { get; set; }
        public UICmd SaveGSSettingsCmd { get; set; }
    }



    public class DBConnection : ObservableObject
    {
        public static List<DBConnection> ListIDs = new();

        private string presetName = "";
        private string type = SettingsVM.dBConnectionTypes[0];
        private string sourceName = "";
        private string catalogName = "";
        private string pCName = "";
        private string iP6Address = "";
        private int port = 1433;
        private string userID = "";
        private string password = "";
        private bool isActive = false;

        public DBConnection() { PresetName = "Preset"; }
        public string PresetName
        {
            get { return presetName; }
            set
            {
                if (value != presetName)
                {
                    string tempID = value; bool isUnique = true; int nr = 1; string defID = tempID;
                    foreach (DBConnection _dbConnection in SettingsVM.Instance.DBList) { if (_dbConnection.PresetName == tempID) { isUnique = false; break; } }
                    if (Basics.SubStr(defID, -2, 1) == "_") { defID = Basics.SubStr(defID, 0, defID.Length - 2); }
                    while (!isUnique)
                    {
                        isUnique = true;
                        tempID = defID + "_" + nr.ToString();
                        foreach (DBConnection _dbConnection in SettingsVM.Instance.DBList)
                        {
                            if (_dbConnection.PresetName == tempID) { isUnique = false; nr++; if (nr == int.MaxValue) { isUnique = true; } break; }
                        }

                    }
                    presetName = tempID;
                }
                RaisePropertyChanged();
            }
        }
        public string Type
        {
            get { return type; }
            set {
                if (SettingsVM.dBConnectionTypes.Contains(value))
                {
                    type = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(IsType0));
                    RaisePropertyChanged(nameof(IsType1));
                }
            }
        }
        public string SourceName { get { return sourceName; } set { sourceName = value; RaisePropertyChanged(); } }
        public string CatalogName { get { return catalogName; } set { catalogName = value; RaisePropertyChanged(); } }
        public string PCName { get { return pCName; } set { pCName = value; RaisePropertyChanged(); } }
        public string IP6Address { get { return iP6Address; } set { iP6Address = value; RaisePropertyChanged(); } }
        public int Port { get { return port; } set { port = value; RaisePropertyChanged(); } }
        public string UserID { get { return userID; } set { userID = value; RaisePropertyChanged(); } }
        public string Password { get { return password; } set { password = value; RaisePropertyChanged(); } }
        public bool IsActive
        {
            get { return isActive; }
            set
            {
                if (value != isActive)
                {
                    if (value) { foreach (DBConnection _dbCon in SettingsVM.Instance.DBList) { if (_dbCon.IsActive) { _dbCon.isActive = false; } } }
                    isActive = value;
                    SettingsVM.Instance.UpdateActiveDBConnection();
                    RaisePropertyChanged();
                }
            }
        }
        [JsonIgnore] public Visibility IsType0 { get { if (Type == SettingsVM.dBConnectionTypes[0]) { return Visibility.Visible; } else { return Visibility.Collapsed; } } }
        [JsonIgnore] public Visibility IsType1 { get { if (Type == SettingsVM.dBConnectionTypes[1]) { return Visibility.Visible; } else { return Visibility.Collapsed; } } }
    }



    public class DisBotPreset : ObservableObject
    {
        public static List<DisBotPreset> ListIDs = new();

        private string presetName = "";
        private DiscordBot disBot = SettingsVM.discordBotList[0];
        private string disBotName = SettingsVM.discordBotList[0].Name;
        private long serverID = Driver.DiscordIDMinValue;
        private long channelID = Driver.DiscordIDMinValue;
        private long adminRoleID = Driver.DiscordIDMinValue;
        private int charLimit = 2000;
        private bool isActive = false;

        public DisBotPreset() { PresetName = "Preset"; }
        public string PresetName
        {
            get { return presetName; }
            set
            {
                if (value != presetName)
                {
                    string tempID = value; bool isUnique = true; int nr = 1; string defID = tempID;
                    foreach (DisBotPreset _disBotPre in SettingsVM.Instance.DisBotPresetList) { if (_disBotPre.PresetName == tempID) { isUnique = false; break; } }
                    if (Basics.SubStr(defID, -2, 1) == "_") { defID = Basics.SubStr(defID, 0, defID.Length - 2); }
                    while (!isUnique)
                    {
                        isUnique = true;
                        tempID = defID + "_" + nr.ToString();
                        foreach (DisBotPreset _disBotPre in SettingsVM.Instance.DisBotPresetList)
                        {
                            if (_disBotPre.PresetName == tempID) { isUnique = false; nr++; if (nr == int.MaxValue) { isUnique = true; } break; }
                        }

                    }
                    presetName = tempID;
                }
                RaisePropertyChanged();
            }
        }
        [JsonIgnore] public DiscordBot DisBot { get { return disBot; } set { if (SettingsVM.discordBotList.Contains(value)) { disBot = value; disBotName = disBot.Name; } } }
        public string DisBotName
        {
            get { return disBotName; }
            set { DiscordBot _disBot = DiscordBot.GetByName(value); if (_disBot.Name == value) { disBotName = value; disBot = _disBot; RaisePropertyChanged(); } }
        }
        public long ServerID
        {
            get { return serverID; }
            set { if (!Driver.IsValidDiscordID(value)) { serverID = Driver.DiscordIDMinValue; } else { serverID = value; } }
        }
        public long ChannelID
        {
            get { return channelID; }
            set { if (!Driver.IsValidDiscordID(value)) { channelID = Driver.DiscordIDMinValue; } else { channelID = value; } }
        }
        public long AdminRoleID
        {
            get { return adminRoleID; }
            set { if (!Driver.IsValidDiscordID(value)) { adminRoleID = Driver.DiscordIDMinValue; } else { adminRoleID = value; } }
        }
        public int CharLimit
        {
            get { return charLimit; }
            set { if (value < 0) { charLimit = 0; } else { charLimit = value; } }
        }
        public bool IsActive
        {
            get { return isActive; }
            set
            {
                if (value != isActive)
                {
                    if (value) { foreach (DisBotPreset _disBotPre in SettingsVM.Instance.DisBotPresetList) { if (_disBotPre.IsActive) { _disBotPre.isActive = false; } } }
                    isActive = value;
                    SettingsVM.Instance.UpdateActiveDiscordBot();
                    RaisePropertyChanged();
                }
            }
        }
    }



    public class GSheet : ObservableObject
    {
        public static List<GSheet> ListIDs = new();

        private string name = "";
        private string docid = "";
        private string sheetid = "";
        private string range = "";

        public GSheet() { }

        public string Name { get { return name; } set { name = value; RaisePropertyChanged(); } }
        public string DocID { get { return docid; } set { docid = value; RaisePropertyChanged(); } }
        public string SheetID { get { return sheetid; } set { sheetid = value; RaisePropertyChanged(); } }
        public string Range { get { return range; } set { range = value; RaisePropertyChanged(); } }
    }



    public class DiscordBot
    {
        public static readonly string DefaultName = "";

        private string name = DefaultName;
        private string token;
        private long discordID = Basics.NoID;

        public DiscordBot() { }

        public string Name { get { return name; } set { if (value != DefaultName) { name = value; } } }
        [JsonIgnore] public string Token { get { return token; } set { token = value; } }
        public long DiscordID { get { return discordID; } set { discordID = value; } }

        public static DiscordBot GetByName(string _name)
        {
            foreach (DiscordBot _disBot in SettingsVM.discordBotList)
            {
                if (_disBot.Name == _name) { return _disBot; }
            }
            return new DiscordBot();
        }
    }
}
