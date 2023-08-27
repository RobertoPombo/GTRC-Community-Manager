using Discord.Commands;
using System.Linq;
using Database;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections;

using GTRC_Community_Manager;
using static System.Net.Mime.MediaTypeNames;

namespace Scripts
{
    public class SignInOutBot
    {
        public static SignInOutBot? Instance;
        public DiscordSocketClient _client;
        private CommandService _commands;
        //private InteractionService _interactions;
        private IServiceProvider _services;
        public DisBotPreset Settings;

        public SignInOutBot(DisBotPreset settings)
        {
            Instance = this;
            Settings = settings;
            _client = new DiscordSocketClient();
            _commands = new CommandService();
            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .BuildServiceProvider();
            _client.Log += _client_Log;
        }

        public async Task RunBotAsync()
        {
            await RegisterCommandsAsync();
            await _client.LoginAsync(TokenType.Bot, Settings.DisBot.Token);
            await _client.StartAsync();
        }

        public void StopBot()
        {
            if (_client is not null) { _client.Dispose(); }
        }

        private Task _client_Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }

        public async Task RegisterCommandsAsync()
        {
            _client.MessageReceived += HandleCommandAsync;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            var UserMessage = arg as SocketUserMessage;
            var context = new SocketCommandContext(_client, UserMessage);
            int argPos = 0;
            List<string> Tags = new();
            List<string> TagList = TagDiscordID(Settings.DisBot.DiscordID);
            foreach (var _tag in TagList) { Tags.Add(_tag + " "); }
            Tags.Add("!");
            foreach (string tag in Tags)
            {
                if (UserMessage.HasStringPrefix(tag, ref argPos))
                {
                    var result = await _commands.ExecuteAsync(context, argPos, _services);
                    if (!result.IsSuccess && UserMessage is not null && Commands.Instance is not null)
                    {
                        Commands.ChannelEntrylist = UserMessage.Channel;
                        Commands.Instance.IsAdmin = RaceControl.Statics.ExistsUniqProp(Driver.Statics.GetByUniqProp((long)UserMessage.Author.Id, 1).ID);
                        await Commands.Instance.Help();
                        await UserMessage.DeleteAsync();
                    }
                }
            }
        }

        public static List<string> TagDiscordID(long _discordID)
        {
            List<string> TagList = new()
            {
                "<@" + _discordID.ToString() + ">",
                "<@!" + _discordID.ToString() + ">"
            };
            return TagList;
        }
    }

    public class Commands : ModuleBase<SocketCommandContext>
    {
        public static Commands? Instance;
        public static SignInOutBot? iSioBot;
        public static DisBotPreset? Settings;
        public static PreSeasonVM? iPreSVM = PreSeasonVM.Instance;
        public static ISocketMessageChannel? ChannelEntrylist;
        public static ISocketMessageChannel? ChannelTrackreport;
        public static ISocketMessageChannel? ChannelNotifyAdmins;
        public static DiscordMessages discordMessages = new(true);
        public static MissingDiscordIDs missingDiscordIDs = new(true);
        public static Emoji emojiSuccess = new("✅");
        public static Emoji emojiFail = new("❌");
        public static Emoji emojiWTF = new("🤷‍♀️");
        public static Emoji emojiSleep = new("😴");
        public static Emoji emojiShocked = new("😱");
        public static Emoji emojiRaceCar = new("🏎️");
        public static Emoji emojiPartyFace = new("🥳");
        public static Emoji emojiCry = new("😭");
        public static Emoji emojiThinking = new("🤔");
        public static string adminRoleTag = "<@&>";
        public static bool IsRunning = false;
        public static readonly Random random = new();

        public bool isError = false;
        public string LogText = "";
        public SocketUserMessage? UserMessage = null;
        public bool RegisterType = true;
        public bool IsAdmin = false;
        public long DiscordID_Author = Basics.NoID;
        public long DiscordID_Driver = Basics.NoID;
        public List<long> DiscordIDs_Drivers = new();
        public string strDiscordID_Driver = Basics.NoID.ToString();
        public int CarID = Basics.NoID;
        public int EventID = Basics.NoID;
        public int DriverID = Basics.NoID;
        public int EntryID = Basics.NoID;

        public Commands()
        {
            Instance = this;
            if (SignInOutBot.Instance is not null) { iSioBot = SignInOutBot.Instance; Settings = iSioBot.Settings; }
            if (PreSeasonVM.Instance is not null) { iPreSVM = PreSeasonVM.Instance; }
            adminRoleTag = "<@&" + Settings?.AdminRoleID.ToString() + ">";
            if (Settings is not null)
            {
                ChannelEntrylist = iSioBot?._client.GetGuild((ulong)Settings.ServerID)?.GetTextChannel((ulong)Settings.ChannelIDEntrylist);
                ChannelTrackreport = iSioBot?._client.GetGuild((ulong)Settings.ServerID)?.GetTextChannel((ulong)Settings.ChannelIDTrackreport);
                ChannelNotifyAdmins = iSioBot?._client.GetGuild((ulong)Settings.ServerID)?.GetTextChannel((ulong)Settings.ChannelIDNotifyAdmins);
            }
        }



        /*[Command("abmelden")]
        public async Task SignInOutCmd()
        {
            if (iPreSVM is not null)
            {
                SetDefaultProperties();
                await ParseEventID((Event.GetNextEvent(iPreSVM.CurrentSeasonID, DateTime.Now).EventNr).ToString());
                var component = new ComponentBuilder();
                List<dynamic> ListMenuEvents = new() { new SelectMenuBuilder() { CustomId = "MenuEvents0", Placeholder = "Event auswählen" } };
                List<dynamic> ListMenuEntries = new() { new SelectMenuBuilder() { CustomId = "MenuEntries0", Placeholder = "Entry auswählen" } };
                List<Event> listEvents = Event.Statics.GetBy(nameof(Event.SeasonID), iPreSVM.CurrentSeasonID);
                int optionCount = 0;
                int eventNr0 = 0;
                for (int nr = 0; nr < listEvents.Count; nr++) { if (listEvents[nr].ID == EventID) { eventNr0 = nr; break; } }
                for (int nr = eventNr0; nr < listEvents.Count; nr++)
                {
                    (ListMenuEvents, optionCount, component) = AddOption2Menu(listEvents[nr].Name, listEvents[nr].ID.ToString(), ListMenuEvents, optionCount, component, "MenuEvents", "Event auswählen");
                }
                Entry _entry = Entry.Statics.GetByID(EntryID);
                if (IsAdmin)
                {
                    for (int nr = 0; nr < eventNr0; nr++)
                    {
                        (ListMenuEvents, optionCount, component) = AddOption2Menu(listEvents[nr].Name, listEvents[nr].ID.ToString(), ListMenuEvents, optionCount, component, "MenuEvents", "Event auswählen");
                    }
                    component.WithSelectMenu(ListMenuEvents[^1]);
                    var linqList = from _linqEntry in Entry.Statics.List
                                   where _linqEntry.SeasonID == iPreSVM.CurrentSeasonID
                                   orderby _linqEntry.RaceNumber
                                   select _linqEntry;
                    List<Entry> listEntries = linqList.Cast<Entry>().ToList();
                    optionCount = 0;
                    int entryNr0 = 0;
                    for (int nr = 0; nr < listEntries.Count; nr++) { if (listEntries[nr].ID == _entry.ID) { entryNr0 = nr; break; } }
                    for (int nr = entryNr0; nr < listEntries.Count; nr++)
                    {
                        List<DriversEntries> listDriversEntries = DriversEntries.Statics.GetBy(nameof(DriversEntries.EntryID), listEntries[nr].ID);
                        List<Driver> listDrivers = new();
                        foreach(DriversEntries _driverEntry in listDriversEntries) { listDrivers.Add(Driver.Statics.GetByID(_driverEntry.DriverID)); }
                        string value = "#" + listEntries[nr].RaceNumber.ToString() + " - " + Driver.DriverList2String(listDrivers, nameof(Driver.FullName));
                        (ListMenuEntries, optionCount, component) = AddOption2Menu(value, listEntries[nr].ID.ToString(), ListMenuEntries, optionCount, component, "MenuEntries", "Entry auswählen");
                    }
                    for (int nr = 0; nr < entryNr0; nr++)
                    {
                        (ListMenuEntries, optionCount, component) = AddOption2Menu("#" + listEntries[nr].RaceNumber.ToString(), listEntries[nr].ID.ToString(), ListMenuEntries, optionCount, component, "MenuEntries", "Entry auswählen");
                    }
                    component.WithSelectMenu(ListMenuEntries[^1]);
                }
                else { component.WithSelectMenu(ListMenuEvents[^1]); }
                var ButtonSignIn = new ButtonBuilder() { Label = "Anmelden", CustomId = "ButtonSignIn", Style = ButtonStyle.Primary };
                var ButtonSignOut = new ButtonBuilder() { Label = "Abmelden", CustomId = "ButtonSignOut", Style = ButtonStyle.Secondary };
                component.WithButton(ButtonSignIn);
                component.WithButton(ButtonSignOut);
                string text = "Von einem Event an-/abmelden";
                if (_entry.ID == Basics.NoID) { text = "#" + _entry.RaceNumber.ToString() + " v" + text[1..]; }
                await ReplyAsync(text, components: component.Build());
            }
        }

        [ComponentInteraction("ButtonSignIn")]
        public async Task SignInCmd()
        {
            RegisterType = true;
            await SignInOut();
        }

        [ComponentInteraction("ButtonSignOut")]
        public async Task SignOutCmd()
        {
            RegisterType = false;
            await SignInOut();
        }

        public (List<dynamic>, int, ComponentBuilder) AddOption2Menu(string value, string name, List<dynamic> ListMenu, int optionCount, ComponentBuilder component, string id, string placeholder)
        {
            int countListMenu = ListMenu.Count;
            optionCount++;
            if (optionCount > 25)
            {
                ListMenu.Add(new SelectMenuBuilder() { CustomId = id + countListMenu.ToString(), Placeholder = placeholder });
                optionCount = 1; countListMenu++;
                component.WithSelectMenu(ListMenu[countListMenu - 2]);
            }
            ListMenu[countListMenu - 1].AddOption(value, name);
            return (ListMenu, optionCount, component);
        }*/

        [Command("abmelden")]
        public async Task SignOutCmd_alt()
        {
            if (Settings is not null)
            {
                SetDefaultProperties();
                RegisterType = false;
                await ParseEventID(Event.GetNextEvent(Settings.CurrentSeasonID, DateTime.Now).EventNr.ToString());
                await SignInOut();
            }
        }

        [Command("anmelden")]
        public async Task SignInCmd_alt()
        {
            if (Settings is not null)
            {
                SetDefaultProperties();
                RegisterType = true;
                await ParseEventID(Event.GetNextEvent(Settings.CurrentSeasonID, DateTime.Now).EventNr.ToString());
                await SignInOut();
            }
        }

        [Command("abmelden")]
        public async Task SignOutCmd_alt(string strEventNr)
        {
            SetDefaultProperties();
            RegisterType = false;
            await ParseEventID(strEventNr);
            await SignInOut();
        }

        [Command("anmelden")]
        public async Task SignInCmd_alt(string strEventNr)
        {
            SetDefaultProperties();
            RegisterType = true;
            await ParseEventID(strEventNr);
            await SignInOut();
        }

        [Command("abmelden")]
        public async Task SignOutCmd_alt(string strEventNr, string strDiscordID)
        {
            SetDefaultProperties();
            RegisterType = false;
            await ParseEventID(strEventNr); await ParseDiscordID(strDiscordID);
            await SignInOut();
        }

        [Command("anmelden")]
        public async Task SignInCmd_alt(string strEventNr, string strDiscordID)
        {
            SetDefaultProperties();
            RegisterType = true;
            await ParseEventID(strEventNr); await ParseDiscordID(strDiscordID);
            await SignInOut();
        }

        [Command("zurückziehen")]
        public async Task PullOutCmd_alt()
        {
            if (Settings is not null)
            {
                SetDefaultProperties();
                RegisterType = false;
                await ParseEventID(Event.GetNextEvent(Settings.CurrentSeasonID, DateTime.Now).EventNr.ToString());
                await PullInOut();
            }
        }

        [Command("zurückziehen")]
        public async Task PullOutCmd_alt(string strDiscordID)
        {
            if (Settings is not null)
            {
                SetDefaultProperties();
                RegisterType = false;
                await ParseEventID(Event.GetNextEvent(Settings.CurrentSeasonID, DateTime.Now).EventNr.ToString());
                await ParseDiscordID(strDiscordID);
                await PullInOut();
            }
        }

        [Command("dochnichtzurückziehen")]
        public async Task UndoPullOutCmd_alt(string strDiscordID)
        {
            if (Settings is not null)
            {
                SetDefaultProperties();
                RegisterType = true;
                await ParseEventID(Event.GetNextEvent(Settings.CurrentSeasonID, DateTime.Now).EventNr.ToString());
                await ParseDiscordID(strDiscordID);
                await PullInOut();
            }
        }

        [Command("fahrzeugwechsel")]
        public async Task ChangeCarCmd_alt(string strCarNr)
        {
            if (Settings is not null)
            {
                SetDefaultProperties();
                await ParseEventID(Event.GetNextEvent(Settings.CurrentSeasonID, DateTime.Now).EventNr.ToString());
                await ParseCarID(strCarNr);
                await ChangeCar();
            }
        }

        [Command("fahrzeugwechsel")]
        public async Task ChangeCarCmd_alt(string strCarNr, string strDiscordID)
        {
            if (Settings is not null)
            {
                SetDefaultProperties();
                await ParseEventID(Event.GetNextEvent(Settings.CurrentSeasonID, DateTime.Now).EventNr.ToString());
                await ParseCarID(strCarNr); await ParseDiscordID(strDiscordID);
                await ChangeCar();
            }
        }

        [Command("kalender")]
        public async Task ShowEventsCmd_alt()
        {
            SetDefaultProperties();
            await ShowEvents();
            await UserMessage!.DeleteAsync();
        }

        [Command("fahrzeugliste")]
        public async Task ShowCarsCmd_alt()
        {
            if (Settings is not null)
            {
                SetDefaultProperties();
                await ParseEventID(Event.GetNextEvent(Settings.CurrentSeasonID, DateTime.Now).EventNr.ToString());
                await ShowCars();
                await UserMessage!.DeleteAsync();
            }
        }

        [Command("bop")]
        public async Task ShowBoPCmd_alt()
        {
            if (Settings is not null)
            {
                SetDefaultProperties();
                await ParseEventID(Event.GetNextEvent(Settings.CurrentSeasonID, DateTime.Now).EventNr.ToString());
                await ShowBoP();
                await UserMessage!.DeleteAsync();
            }
        }

        [Command("bop")]
        public async Task ShowBoPCmd_alt(string strEventNr)
        {
            SetDefaultProperties();
            await ParseEventID(strEventNr);
            await ShowBoP();
            await UserMessage!.DeleteAsync();
        }

        [Command("starterfeld")]
        public async Task ShowStarterfeldCmd_alt()
        {
            if (Settings is not null)
            {
                SetDefaultProperties();
                await ParseEventID(Event.GetNextEvent(Settings.CurrentSeasonID, DateTime.Now).EventNr.ToString());
                var tempReply = await ReplyAsync(":sleeping:");
                while (MainWindow.CheckExistingSqlThreads()) { Thread.Sleep(200 + random.Next(100)); } IsRunning = true;
                await ShowStartingGrid(false, false);
                await tempReply.DeleteAsync(); IsRunning = false;
                await UserMessage!.DeleteAsync();
            }
        }

        [Command("starterfeld")]
        public async Task ShowStarterfeldCmd_alt(string strEventNr)
        {
            SetDefaultProperties();
            await ParseEventID(strEventNr);
            var tempReply = await ReplyAsync(":sleeping:");
            while (MainWindow.CheckExistingSqlThreads()) { Thread.Sleep(200 + random.Next(100)); }
            IsRunning = true;
            await ShowStartingGrid(false, false);
            await tempReply.DeleteAsync(); IsRunning = false;
            await UserMessage!.DeleteAsync();
        }

        [Command("elo")]
        public async Task ShowELOCmd_alt()
        {
            if (Settings is not null)
            {
                SetDefaultProperties();
                await ShowELO();
                await UserMessage!.DeleteAsync();
            }
        }

        [Command("sr")]
        public async Task ShowSRCmd_alt()
        {
            if (Settings is not null)
            {
                SetDefaultProperties();
                await ShowSR();
                await UserMessage!.DeleteAsync();
            }
        }



        public async Task ErrorResponse()
        {
            if (!isError)
            {
                await ReplyAsync(LogText);
                await UserMessage!.AddReactionAsync(emojiFail);
                isError = true;
            }
        }

        public void SetDefaultProperties()
        {
            isError = false;
            ChannelEntrylist = Context.Channel;
            UserMessage = Context.Message;
            DiscordID_Author = (long)Context.Message.Author.Id;
            DiscordID_Driver = (long)Context.Message.Author.Id;
            IsAdmin = RaceControl.Statics.ExistsUniqProp(Driver.Statics.GetByUniqProp(DiscordID_Author, 1).ID);
        }

        public async Task ParseCarID(string strCarNr)
        {
            LogText = "Bitte eine gültige Fahrzeugnummer angeben.";
            if (int.TryParse(strCarNr, out int intCarNr))
            {
                if (Car.Statics.ExistsUniqProp(intCarNr)) { CarID = Car.Statics.GetByUniqProp(intCarNr).ID; }
                else { await ErrorResponse(); await ShowCars(); }
            }
            else { await ErrorResponse(); await ShowCars(); }
        }

        public async Task ParseEventID(string strEventNr)
        {
            if (int.TryParse(strEventNr, out int intEventNr) && Settings is not null)
            {
                List<Event> listEvents = Event.SortByDate(Event.Statics.GetBy(nameof(Event.SeasonID), Settings.CurrentSeasonID));
                LogText = "Bitte eine Event-Nr zwischen 1 und " + listEvents.Count.ToString() + " angeben.";
                if (intEventNr < 1) { await ErrorResponse(); await ShowEvents(); }
                else if (intEventNr > listEvents.Count) { await ErrorResponse(); await ShowEvents(); }
                else { EventID = listEvents[intEventNr - 1].ID; }
            }
            else { LogText = "Bitte eine gültige Event-Nr angeben."; await ErrorResponse(); }
        }

        public async Task ParseDiscordID(string strDiscordID)
        {
            if (Int64.TryParse(strDiscordID, out long longDiscordID_Driver)) { DiscordID_Driver = longDiscordID_Driver; }
            else { LogText = "Bitte eine gültige Discord-ID oder Startnummer angeben."; await ErrorResponse(); }
        }

        public async Task ParseDriverID()
        {
            DriverID = Driver.Statics.GetByUniqProp(DiscordID_Driver, 1).ID;
            if (DriverID == Basics.NoID)
            {
                if (DiscordID_Driver == DiscordID_Author) { LogText = "Du bist nicht in der Datenbank eingetragen. Vermutlich haben die " + adminRoleTag + " nur vergessen, deine Discord-ID zu speichern."; await ErrorResponse(); }
                else { LogText = "Der Fahrer ist nicht in der Datenbank eingetragen. " + adminRoleTag + "Möglicherweise fehlt seine Discord-ID."; await ErrorResponse(); }
            }
        }

        public async Task ParseEntryID()
        {
            if (iPreSVM is not null && Settings is not null)
            {
                if (Driver.IsValidDiscordID(DiscordID_Driver))
                {
                    if (DiscordID_Driver == Settings.DisBot.DiscordID)
                    {
                        await UserMessage!.AddReactionAsync(emojiRaceCar);
                        if (RegisterType) { await UserMessage.AddReactionAsync(emojiPartyFace); }
                        else { await UserMessage.AddReactionAsync(emojiCry); }
                    }
                    else
                    {
                        await ParseDriverID();
                        if (DriverID > Basics.NoID)
                        {
                            Entry entry = DriversEntries.GetByDriverIDSeasonID(DriverID, Settings.CurrentSeasonID).ObjEntry;
                            if (entry.ID == Basics.NoID)
                            {
                                if (DiscordID_Driver == DiscordID_Author) { LogText = "Du bist noch nicht für die Meisterschaft registriert. Falls du dich gerade erst angemeldet hast, versuche es doch bitte in " + iPreSVM.EntriesUpdateRemTime + " erneut. " + adminRoleTag + " schaut euch das Problem bitte an."; await ErrorResponse(); }
                                else { LogText = "Der Fahrer ist nicht für die Meisterschaft registriert. Die Datenbank wird das nächste Mal in " + iPreSVM.EntriesUpdateRemTime + " synchronisiert."; await ErrorResponse(); }
                            }
                            else { EntryID = entry.ID; SetDiscordIDs_Drivers(); }
                        }
                    }
                }
                else if (int.TryParse(DiscordID_Driver.ToString(), out int _raceNumber))
                {
                    Entry entry = Entry.Statics.GetByUniqProp(new List<dynamic>() { Settings.CurrentSeasonID, _raceNumber });
                    if (entry.ID == Basics.NoID) { LogText = "Die Startnummer " + _raceNumber.ToString() + " ist noch nicht für die Meisterschaft registriert. Falls du dich gerade erst angemeldet hast, versuche es doch bitte in " + iPreSVM.EntriesUpdateRemTime + " erneut. " + adminRoleTag + " schaut euch das Problem bitte an."; await ErrorResponse(); }
                    else { EntryID = entry.ID; CheckAuthorInEntry(); SetDiscordIDs_Drivers(); }
                }
                else { LogText = "Bitte eine gültige Discord-ID oder Startnummer angeben."; await ErrorResponse(); }
            }
        }

        public void CheckAuthorInEntry()
        {
            List<DriversEntries> _driverEntries = DriversEntries.Statics.GetBy(nameof(DriversEntries.EntryID), EntryID);
            foreach (DriversEntries _driverEntry in _driverEntries)
            {
                if (_driverEntry.ObjDriver.DiscordID == DiscordID_Author) { DiscordID_Driver = DiscordID_Author; break; }
            }
        }

        public void SetDiscordIDs_Drivers()
        {
            DiscordIDs_Drivers = GetDiscordIDs_Drivers(EntryID);
        }

        public static List<long> GetDiscordIDs_Drivers(int _entryID)
        {
            List<long> discordIDs_Drivers = new();
            List<DriversEntries> _driverEntries = DriversEntries.Statics.GetBy(nameof(DriversEntries.EntryID), _entryID);
            foreach (DriversEntries _driverEntry in _driverEntries)
            {
                if (_driverEntry.ObjDriver.ID != Basics.NoID && Driver.IsValidDiscordID(_driverEntry.ObjDriver.DiscordID))
                {
                    discordIDs_Drivers.Add(_driverEntry.ObjDriver.DiscordID);
                }
            }
            return discordIDs_Drivers;
        }

        public static string TagDiscordIDs(List<long> listDiscordIDs, bool mobileType)
        {
            string tagText= "";
            foreach (long _discordID in listDiscordIDs) { tagText += TagDiscordID(_discordID, mobileType); }
            return tagText;
        }

        public static string TagDiscordID(long _discordID, bool mobileType)
        {
            string tagText = "";
            if (mobileType) { tagText += "<@!" + _discordID.ToString() + "> "; }
            else { tagText += "<@" + _discordID.ToString() + "> "; }
            return tagText;
        }

        public async Task Help()
        {
            string text = "**Befehle**\n\n";
            text += "`!anmelden` | Für das nächste Event anmelden\n";
            text += "`!anmelden 4` | Für das 4. Event anmelden\n";
            text += "`!abmelden` | Vom nächsten Event abmelden\n";
            text += "`!abmelden 4` | Vom 4. Event abmelden\n";
            text += "`!zurückziehen` | Aus der aktuellen Meisterschaft zurückziehen\n";
            text += "`!fahrzeugwechsel 10` | Auf das Auto 10 wechseln\n";
            text += "`!starterfeld` | Starterfeld, Warteliste & Abmeldungen des nächsten Events anzeigen\n";
            text += "`!starterfeld 4` | Starterfeld, Warteliste & Abmeldungen des 4. Events anzeigen\n";
            text += "`!bop` | BoP des nächsten Events anzeigen\n";
            text += "`!bop 4` | BoP des 4. Events anzeigen\n";
            text += "`!kalender` | Rennkalender der aktuellen Saison anzeigen\n";
            text += "`!fahrzeugliste` | Fahrzeugliste anzeigen";
            if (IsAdmin)
            {
                text += "\n\n**Admin-Befehle**\n\n";
                text += "`!anmelden 4 612` | StartNr/DiscordID 612 für das 4. Event anmelden\n";
                text += "`!abmelden 4 612` | StartNr/DiscordID 612 vom 4. Event abmelden\n";
                text += "`!zurückziehen 612` | StartNr/DiscordID 612 aus der aktuellen Meisterschaft entfernen\n";
                text += "`!dochnichtzurückziehen 612` | Rückzug der StartNr/DiscordID 612 aus der aktuellen Meisterschaft rückgängig machen\n";
                text += "`!fahrzeugwechsel 10 612` | StartNr/DiscordID 612 auf das Auto 10 umschreiben";
                text += "`!elo 612` | ELO-Rating von StartNr/DiscordID 612 ausgeben";
                text += "`!sr 10 612` | Safety-Rating von StartNr/DiscordID 612 ausgeben";
            }
            text += "\n\nStatt das `!` zu verwenden kannst du mich auch direkt mit `@` ansprechen.";
            await SendMessage(text, false);
        }

        public static async Task NotifyEntry(Entry entry, string message, string delimiter)
        {
            List<long> discordIDs_Drivers = GetDiscordIDs_Drivers(entry.ID);
            if (discordIDs_Drivers.Count > 0)
            {
                message = message.Replace(delimiter, TagDiscordIDs(discordIDs_Drivers, false));
                await SendMessage(message, false, false);
                missingDiscordIDs.ListRemove(entry.ID);
            }
            else
            {
                message = message.Replace(delimiter, "@Teilnehmer #" + entry.RaceNumber.ToString() + " (Discord-ID fehlt)\n ");
                await NotifyAdmins(message);
                missingDiscordIDs.ListAdd(entry.ID);
            }
        }

        public static async Task NotifyAdmins(string message)
        {
            IsRunning = true;
            if (ChannelNotifyAdmins is null && Settings is not null)
            {
                ChannelNotifyAdmins = iSioBot?._client?.GetGuild((ulong)Settings.ServerID)?.GetTextChannel((ulong)Settings.ChannelIDNotifyAdmins);
            }
            if (ChannelNotifyAdmins is not null) { await ChannelNotifyAdmins.SendMessageAsync(message); }
            IsRunning = false;
        }

        public async Task ChangeCar()
        {
            var tempReply = await ReplyAsync(":sleeping:");
            while (MainWindow.CheckExistingSqlThreads()) { Thread.Sleep(200 + random.Next(100)); } IsRunning = true;
            await ParseEntryID();
            if (EntryID != Basics.NoID && CarID != Basics.NoID && Settings is not null && UserMessage is not null)
            {
                if (DiscordID_Author == DiscordID_Driver || IsAdmin)
                {
                    Entry _entry = Entry.Statics.GetByID(EntryID);
                    Event _event = Event.Statics.GetByID(EventID);
                    EntriesDatetimes _entryDatetime = _entry.GetEntriesDatetimesByDate(_event.Date);
                    Car newCar = Car.Statics.GetByID(CarID);
                    Car oldCar = Car.Statics.GetByID(_entryDatetime.CarID);
                    DateTime limitCarChangeCountback = Basics.DateTimeMinValue;
                    List<Event> listEvents = Event.SortByDate(Event.Statics.GetBy(nameof(Event.SeasonID), _event.SeasonID));
                    int eventNr = listEvents.IndexOf(_event);
                    if (eventNr > 0) { limitCarChangeCountback = listEvents[eventNr - 1].Date; }
                    if (_entry.SignOutDate < DateTime.Now)
                    {
                        if (DiscordID_Author == DiscordID_Driver) { LogText = "Du hast dich aus der Meisterschaft zurückgezogen."; }
                        else { LogText = "Der Teilnehmer hat sich aus der Meisterschaft zurückgezogen."; }
                        await ErrorResponse();
                    }
                    else if (_entryDatetime.CarID == CarID)
                    {
                        if (DiscordID_Author == DiscordID_Driver) { await ReplyAsync("Du bist doch schon auf dem " + newCar.Name + " angemeldet."); }
                        else { await ReplyAsync("Der Teilnehmer ist bereits auf dem " + newCar.Name + " angemeldet."); }
                        await UserMessage.AddReactionAsync(emojiThinking);
                    }
                    
                    else if (_entry.CarChangeCount(limitCarChangeCountback) >= _entry.ObjSeason.CarChangeLimit &&
                        (!_event.ObjSeason.GroupCarLimits || newCar.Category != oldCar.Category || newCar.Manufacturer != oldCar.Manufacturer))
                    {
                        if (DiscordID_Author == DiscordID_Driver) { LogText = "Du kannst dein Fahrzeug nicht mehr wechseln."; }
                        else { LogText = "Dieser Teilnehmer kann das Fahrzeug nicht mehr wechseln."; }
                        await ErrorResponse();
                    }
                    else
                    {
                        EntriesDatetimes newEntryDate = EntriesDatetimes.GetAnyByUniqProp(EntryID, DateTime.Now);
                        newEntryDate.CarID = CarID;
                        _ = EntriesDatetimes.Statics.WriteSQL(newEntryDate);
                        await ShowStartingGrid(true, true);
                        await UserMessage.AddReactionAsync(emojiSuccess);
                    }
                }
                else
                {
                    LogText = "Netter Versuch, aber du kannst nur deine eigene Fahrzeugwahl ändern :stuck_out_tongue:";
                    await ErrorResponse();
                }
            }
            await tempReply.DeleteAsync();
            IsRunning = false;
        }

        public async Task SignInOut()
        {
            var tempReply = await ReplyAsync(":sleeping:");
            while (MainWindow.CheckExistingSqlThreads()) { Thread.Sleep(200 + random.Next(100)); } IsRunning = true;
            await ParseEntryID();
            if (EntryID != Basics.NoID && EventID != Basics.NoID && Settings is not null && UserMessage is not null)
            {
                if (DiscordID_Author == DiscordID_Driver || IsAdmin)
                {
                    EventsEntries _eventsEntries = EventsEntries.GetAnyByUniqProp(EntryID, EventID);
                    Event _event = _eventsEntries.ObjEvent;
                    Entry _entry = _eventsEntries.ObjEntry;
                    if (_entry.SignOutDate < DateTime.Now)
                    {
                        if (DiscordID_Author == DiscordID_Driver) { LogText = "Du hast dich aus der Meisterschaft zurückgezogen."; }
                        else { LogText = "Der Teilnehmer hat sich aus der Meisterschaft zurückgezogen."; }
                        await ErrorResponse();
                    }
                    else if (_eventsEntries.SignInState == RegisterType)
                    {
                        if (RegisterType && DiscordID_Author == DiscordID_Driver) { await ReplyAsync("Du bist doch schon angemeldet."); }
                        else if (RegisterType && DiscordID_Author != DiscordID_Driver) { await ReplyAsync("Der Teilnehmer ist bereits angemeldet."); }
                        else if (!RegisterType && DiscordID_Author == DiscordID_Driver) { await ReplyAsync("Du bist doch gar nicht angemeldet."); }
                        else if (!RegisterType && DiscordID_Author != DiscordID_Driver) { await ReplyAsync("Der Teilnehmer ist gar nicht angemeldet."); }
                        await UserMessage.AddReactionAsync(emojiThinking);
                    }
                    else if (_event.Date <= DateTime.Now)
                    {
                        LogText = "Leider zu spät. Die Deadline war am " + Basics.Date2String(_event.Date, "DD.MM.YY") + " um " + Basics.Date2String(_event.Date, "hh:mm") + " Uhr.";
                        await ErrorResponse();
                    }
                    else
                    {
                        if (RegisterType) { _eventsEntries.SignInDate = DateTime.Now; }
                        else { _eventsEntries.SignInDate = Basics.DateTimeMaxValue; }
                        EventsEntries.Statics.WriteSQL();
                        await ShowStartingGrid(false, false);
                        await UserMessage.AddReactionAsync(emojiSuccess);
                    }
                }
                else
                {
                    if (RegisterType)
                    {
                        LogText = "Es ist sicher nett gemeint, aber du kannst nur dein eigenes Fahrzeug zu diesem Event anmelden.";
                        await ErrorResponse();
                    }
                    else
                    {
                        LogText = "Netter Versuch, aber du kannst nur dein eigenes Fahrzeug von diesem Event abmelden. :stuck_out_tongue:";
                        await ErrorResponse();
                    }
                }
            }
            await tempReply.DeleteAsync();
            IsRunning = false;
        }

        public async Task PullInOut()
        {
            var tempReply = await ReplyAsync(":sleeping:");
            while (MainWindow.CheckExistingSqlThreads()) { Thread.Sleep(200 + random.Next(100)); } IsRunning = true;
            await ParseEntryID();
            if (EntryID != Basics.NoID && Settings is not null && UserMessage is not null)
            {
                Entry _entry = Entry.Statics.GetByID(EntryID);
                bool currentRegisterState = _entry.SignOutDate > DateTime.Now;
                if (RegisterType) { currentRegisterState = _entry.SignOutDate >= Basics.DateTimeMaxValue; }
                if (currentRegisterState == RegisterType)
                {
                    if (RegisterType && DiscordID_Author == DiscordID_Driver) { await ReplyAsync("Keine Sorge, du bist noch in der Meisterschaft eingeschrieben."); }
                    else if (RegisterType && DiscordID_Author != DiscordID_Driver) { await ReplyAsync("Der Teilnehmer ist noch in der Meisterschaft eingeschrieben."); }
                    else if (!RegisterType && DiscordID_Author == DiscordID_Driver) { await ReplyAsync("Du bist doch schon aus der Meisterschaft abgemeldet."); }
                    else if (!RegisterType && DiscordID_Author != DiscordID_Driver) { await ReplyAsync("Der Teilnehmer hat bereits aus der Meisterschaft zurückgezogen."); }
                    await UserMessage.AddReactionAsync(emojiWTF);
                }
                else if (RegisterType && !IsAdmin && DiscordID_Author == DiscordID_Driver)
                {
                    LogText = "Toll, dass du es dir anders überlegt hast. Aber leider kannst du dich selbst wieder anmelden. Frag doch bitte einen unserer " + adminRoleTag + ", die machen das bestimmt gerne.";
                    await ErrorResponse();
                }
                else if (RegisterType && !IsAdmin && DiscordID_Author != DiscordID_Driver)
                {
                    LogText = "Es ist sicher nett gemeint, aber leider können nur unsere " + adminRoleTag + " einen Rückzug aus der Meisterschaft rückgängig machen.";
                    await ErrorResponse();
                }
                else if (!RegisterType && !IsAdmin && DiscordID_Author != DiscordID_Driver)
                {
                    LogText = "Netter Versuch, aber du kannst nur dein eigenes Fahrzeug von der Meisterschaft abmelden. :stuck_out_tongue:";
                    await ErrorResponse();
                }
                else
                {
                    if (RegisterType) { _entry.SignOutDate = Basics.DateTimeMaxValue; }
                    else { _entry.SignOutDate = DateTime.Now; }
                    EventsEntries.Statics.WriteSQL();
                    Entry.Statics.WriteSQL();
                    await ShowStartingGrid(false, false);
                    await UserMessage.AddReactionAsync(emojiSuccess);
                }
            }
            await tempReply.DeleteAsync();
            IsRunning = false;
        }

        public async Task ShowEvents()
        {
            if (Settings is not null)
            {
                while (MainWindow.CheckExistingSqlThreads()) { Thread.Sleep(200 + random.Next(100)); } IsRunning = true;
                List<Event> listEvents = Event.SortByDate(Event.Statics.GetBy(nameof(Event.SeasonID), Settings.CurrentSeasonID));
                string text = "**Rennkalender " + Settings.CurrentSeasonM.Season.Name + "**\n";
                foreach (Event _event in listEvents)
                {
                    text += (_event.EventNr).ToString() + ".\t";
                    text += Basics.Date2String(_event.Date, "DD.MM.\t");
                    text += _event.ObjTrack.Name_GTRC + "\n";
                }
                await SendMessage(text, false);
                IsRunning = false;
            }
        }

        public async Task ShowCars()
        {
            if (iPreSVM is not null)
            {
                var tempReply = await ReplyAsync(":sleeping:");
                while (MainWindow.CheckExistingSqlThreads()) { Thread.Sleep(200 + random.Next(100)); } IsRunning = true;
                if (EventID != Basics.NoID)
                {
                    Event _event = Event.Statics.GetByID(EventID);
                    iPreSVM.UpdateBoPForEvent(_event);
                }
                string text = "**Fahrzeugliste**\n";
                var linqList = from _car in Car.Statics.List
                               orderby _car.Name
                               select _car;
                List<Car> carList = linqList.Cast<Car>().ToList();
                foreach (Car _car in carList)
                {
                    EventsCars? eventCar = null;
                    if (EventID != Basics.NoID) { eventCar = EventsCars.GetAnyByUniqProp(_car.ID, EventID); }
                    if (_car.Category == "GT3" && (_car.IsLatestModel || IsAdmin))
                    {
                        text += _car.AccCarID.ToString() + "\t";
                        text += _car.Name;
                        text += " (" + _car.Year.ToString() + ")";
                        if (eventCar is not null)
                        {
                            text += " - " + eventCar.CountBoP.ToString() + "/" + eventCar.ObjEvent.ObjSeason.CarLimitRegisterLimit.ToString();
                        }
                        text += "\n";
                    }
                }
                await SendMessage(text, false);
                await tempReply.DeleteAsync();
                IsRunning = false;
            }
        }

        public async Task ShowBoP()
        {
            if (EventID != Basics.NoID && iPreSVM is not null)
            {
                var tempReply = await ReplyAsync(":sleeping:");
                while (MainWindow.CheckExistingSqlThreads()) { Thread.Sleep(200 + random.Next(100)); } IsRunning = true;
                Event _event = Event.Statics.GetByID(EventID);
                iPreSVM.UpdateBoPForEvent(_event);
                IsRunning = false;
                string text = "**BoP für Event " + _event.Name + Basics.Date2String(_event.Date, " (DD.MM.YY)**\n");
                List<EventsCars> eventsCars = EventsCars.SortByCount(EventsCars.GetAnyBy(nameof(EventsCars.EventID), EventID));
                foreach (EventsCars eventCar in eventsCars)
                {
                    if (eventCar.CountBoP > 0)
                    {
                        text += eventCar.CountBoP.ToString() + "x\t";
                        text += eventCar.Ballast.ToString() + " kg\t";
                        //text += _carBoP.Restrictor.ToString() + "%\t";
                        text += eventCar.ObjCar.Name + "\n";
                    }
                }
                await SendMessage(text, false);
                await tempReply.DeleteAsync();
                IsRunning = false;
            }
        }

        public async Task ShowELO()
        {
            if (iPreSVM is not null)
            {
                var tempReply = await ReplyAsync(":sleeping:");
                while (MainWindow.CheckExistingSqlThreads()) { Thread.Sleep(200 + random.Next(100)); }
                IsRunning = true;
                await ParseDriverID();
                if (DriverID > Basics.NoID)
                {
                    string text = "Fahrer: " + Driver.Statics.GetByID(DriverID).FullName;
                    text += "\nELO-Rating: " + Driver.Statics.GetByID(DriverID).EloRating.ToString();
                    await SendMessage(text, false);
                    await tempReply.DeleteAsync();
                }
                IsRunning = false;
            }
        }

        public async Task ShowSR()
        {
            if (iPreSVM is not null)
            {
                var tempReply = await ReplyAsync(":sleeping:");
                while (MainWindow.CheckExistingSqlThreads()) { Thread.Sleep(200 + random.Next(100)); }
                IsRunning = true;
                await ParseDriverID();
                if (DriverID > Basics.NoID)
                {
                    string text = "Fahrer: " + Driver.Statics.GetByID(DriverID).FullName;
                    text += "\nSafety-Rating: " + Driver.Statics.GetByID(DriverID).SafetyRating.ToString();
                    await SendMessage(text, false);
                    await tempReply.DeleteAsync();
                }
                IsRunning = false;
            }
        }

        public async Task ShowStartingGrid(bool printCar, bool printCarChange)
        {
            if (EventID != Basics.NoID && iPreSVM is not null && Settings is not null)
            {
                await CreateStartingGridMessage(EventID, printCar, printCarChange);
            }
        }

        public static async Task CreateStartingGridMessage(int eventID, bool printCar, bool printCarChange)
        {
            if (PreSeasonVM.Instance is not null && iPreSVM is null) { iPreSVM = PreSeasonVM.Instance; }
            if (eventID != Basics.NoID && iPreSVM is not null && Settings is not null)
            {
                Event _event = Event.Statics.GetByID(eventID);
                int SlotsTaken = iPreSVM.ThreadUpdateEntrylistBoP_Int(_event);
                //if (Settings.CurrentSeasonM.DateRegisterLimit < DateTime.Now) { printCarChange = true; }

                List<EventsEntries> listSortPriority = EventsEntries.GetAnyBy(nameof(EventsEntries.EventID), _event.ID);
                var linqList = from _eventEntry in listSortPriority
                               orderby _eventEntry.Priority
                               select _eventEntry;
                listSortPriority = linqList.Cast<EventsEntries>().ToList();
                linqList = from _eventEntry in listSortPriority
                           orderby _eventEntry.ScorePoints descending, _eventEntry.ObjEntry.RaceNumber
                           select _eventEntry;
                List<EventsEntries> listSortRaceNumber = linqList.Cast<EventsEntries>().ToList();

                string text = "**Starterfeld für Event " + _event.Name + Basics.Date2String(_event.Date, " (DD.MM.YY)") + " | "
                    + SlotsTaken.ToString() + "/" + iPreSVM.GetSlotsAvalable(_event.ObjTrack, _event.SeasonID).ToString() + "**\n";
                string textTemp = "";
                int pos = 1;
                foreach (EventsEntries eventEntry in listSortRaceNumber)
                {
                    if (eventEntry.RegisterState && eventEntry.SignInState && eventEntry.IsOnEntrylist)
                    {
                        (textTemp, pos) = AddStartingGridLine(textTemp, pos, eventEntry, printCar, printCarChange, false);
                    }
                }
                if (pos > 1) { text += textTemp; } else { text += "-\n"; }

                textTemp = "";
                pos = 1;
                foreach (EventsEntries eventEntry in listSortPriority)
                {
                    if (eventEntry.RegisterState && eventEntry.SignInState && !eventEntry.IsOnEntrylist)
                    {
                        (textTemp, pos) = AddStartingGridLine(textTemp, pos, eventEntry, printCar, printCarChange, true);
                    }
                }
                if (pos > 1) { text += "\n**Warteliste (" + (pos - 1).ToString() + ")**\n" + textTemp; }

                textTemp = "";
                pos = 1;
                foreach (EventsEntries eventEntry in listSortRaceNumber)
                {
                    if (eventEntry.RegisterState && !eventEntry.SignInState)
                    {
                        (textTemp, pos) = AddStartingGridLine(textTemp, pos, eventEntry, printCar, printCarChange, false);
                    }
                }
                if (pos > 1) { text += "\n**Abmeldungen (" + (pos - 1).ToString() + ")**\n" + textTemp; }

                textTemp = "";
                pos = 1;
                List<Event> listEvents = Event.SortByDate(Event.Statics.GetBy(nameof(Event.SeasonID), _event.SeasonID));
                foreach (EventsEntries eventEntry in listSortRaceNumber)
                {
                    if (!eventEntry.RegisterState && listEvents.Count > 0 && eventEntry.ObjEntry.SignOutDate > listEvents[0].Date)
                    {
                        (textTemp, pos) = AddStartingGridLine(textTemp, pos, eventEntry, printCar, printCarChange, false);
                    }
                }
                if (pos > 1) { text += "\n**Aus der Meisterschaft zurückgezogen (" + (pos - 1).ToString() + ")**\n" + textTemp; }

                await SendMessage(text, true);
            }
        }

        public static (string, int) AddStartingGridLine(string text, int pos, EventsEntries eventEntry, bool printCar, bool printCarChange, bool printPos)
        {
            Entry _entry = eventEntry.ObjEntry;
            EntriesDatetimes entryDatetime = _entry.GetEntriesDatetimesByDate(eventEntry.ObjEvent.Date);
            if (printPos) { text += pos.ToString() + ".\t"; }
            text += _entry.RaceNumber.ToString() + "\t";
            List<Driver> listDrivers = new();
            List<DriversEntries> listDriverEntries = DriversEntries.Statics.GetBy(nameof(DriversEntries.EntryID), _entry.ID);
            foreach (DriversEntries driverEntry in listDriverEntries) { listDrivers.Add(driverEntry.ObjDriver); }
            text += Driver.DriverList2String(listDrivers, nameof(Driver.FullName));
            if (printCar) { text += " | " + entryDatetime.ObjCar.Name; }
            if (printCarChange && _entry.ScorePoints && _entry.ObjSeason.CarChangeLimit < int.MaxValue && _entry.ObjSeason.DateCarChangeLimit < Basics.DateTimeMaxValue)
            {
                int ccCount = _entry.CarChangeCount(eventEntry.ObjEvent.Date);
                if (ccCount >= _entry.ObjSeason.CarChangeLimit) { text += " | **" + ccCount.ToString() + "/" + _entry.ObjSeason.CarChangeLimit.ToString() + "**"; }
                else { text += " | " + ccCount.ToString() + "/" + _entry.ObjSeason.CarChangeLimit.ToString(); }
            }
            if (!eventEntry.ScorePoints)
            {
                if (!_entry.ScorePoints)
                {
                    List<Event> listEvents = Event.SortByDate(Event.Statics.GetBy(nameof(Event.SeasonID), _entry.SeasonID));
                    bool notScorePointsFirstEvent = listEvents.Count > 0 && !EventsEntries.GetAnyByUniqProp(_entry.ID, listEvents[0].ID).ScorePoints;
                    bool exceedsNoShowLimit = _entry.CountNoShow(eventEntry.ObjEvent, false) > _entry.ObjSeason.NoShowLimit;
                    if (!notScorePointsFirstEvent && exceedsNoShowLimit) { text += " | *außer Wertung (wg. Abw. trotz Anm.)*"; }
                    else { text += " | *außer Wertung*"; }
                }
                else
                {
                    text += " | *außer Wertung (wg. Fzglimit)*";
                    if (missingDiscordIDs.entryIDs.Contains(_entry.ID) && GetDiscordIDs_Drivers(_entry.ID).Count > 0)
                    {
                        string delimiter = "#!#";
                        string message = delimiter + PreSeason.CreateNoScorePointsNotification(eventEntry.ObjEvent.ObjSeason, eventEntry.ObjEvent, entryDatetime.ObjCar);
                        _ = Commands.NotifyEntry(_entry, message, delimiter);
                    }
                }
            }
            bool eventBan = false; foreach (Driver _driver in listDrivers) { if (_driver.SafetyRating <= 0) { eventBan = true; } } //Von Event abhängig!!!
            if (eventBan) { text += " | **GESPERRT**"; }
            text += "\n"; pos++;
            return (text, pos);
        }

        public static async Task CreateTrackReportMessage(int _eventID, int chanceOfRain)
        {
            if (ChannelTrackreport is null && Settings is not null)
            {
                ChannelTrackreport = iSioBot?._client?.GetGuild((ulong)Settings.ServerID)?.GetTextChannel((ulong)Settings.ChannelIDTrackreport);
            }
            if (ChannelTrackreport is not null)
            {
                while (MainWindow.CheckExistingSqlThreads()) { Thread.Sleep(200 + random.Next(100)); }
                IsRunning = true;
                Event _event = Event.Statics.GetByID(_eventID);
                List<Session> sesList = _event.GetSessions();
                string text = "**Trackreport " + _event.Name + "**\n";
                text += "\nMittwoch, " + Basics.Date2String(_event.Date, "DD.MM.YY - hh:mm") + " Uhr\n";
                text += "Regenwahrscheinlichkeit: ~" + chanceOfRain.ToString() + "%\n";
                text += "\n**Virtuelle Tageszeiten der Sessions**\n";
                for (int sesNr1 = 0; sesNr1 < sesList.Count; sesNr1++)
                {
                    if (sesList[sesNr1].SessionTypeEnum == Enums.SessionTypeEnum.Practice) { text += "Freies Training"; }
                    else if (sesList[sesNr1].SessionTypeEnum == Enums.SessionTypeEnum.Qualifying) { text += "Qualifying"; }
                    else if (sesList[sesNr1].SessionTypeEnum == Enums.SessionTypeEnum.Race) { text += "Rennen"; }
                    bool showSesNr = false;
                    for (int sesNr2 = 0; sesNr2 < sesList.Count; sesNr2++)
                    {
                        if (sesNr1 != sesNr2 && sesList[sesNr1].SessionType == sesList[sesNr2].SessionType) { showSesNr = true; break; }
                    }
                    if (showSesNr) { text += " " + sesList[sesNr1].SessionNrOfThisType.ToString(); }
                    if (sesList[sesNr1].DayOfWeekendEnum == Enums.DayOfWeekendEnum.Friday) { text += " - Fr, "; }
                    else if (sesList[sesNr1].DayOfWeekendEnum == Enums.DayOfWeekendEnum.Saturday) { text += " - Sa, "; }
                    else if (sesList[sesNr1].DayOfWeekendEnum == Enums.DayOfWeekendEnum.Sunday) { text += " - So, "; }
                    text += sesList[sesNr1].IngameStartTime.ToString() + " Uhr\n";
                }
                text += "\n**Servereinstellungen**\n";
                text += "Temperatur: " + _event.AmbientTemp.ToString() + "°C\n";
                text += "Wolken: " + _event.CloudLevel.ToString() + "%\n";
                text += "Regen: " + _event.RainLevel.ToString() + "%\n";
                text += "Zufälligkeit: " + _event.WeatherRandomness.ToString() + "\n";
                await ChannelTrackreport.SendMessageAsync(text);
                IsRunning = false;
            }
        }

        public static async Task SendMessage(string newMessageContent, bool isStartingGrid, bool deleteLater = true)
        {
            if (ChannelEntrylist is null && Settings is not null)
            {
                ChannelEntrylist = iSioBot?._client?.GetGuild((ulong)Settings.ServerID)?.GetTextChannel((ulong)Settings.ChannelIDEntrylist);
            }
            if (ChannelEntrylist is not null)
            {
                if (deleteLater)
                {
                    foreach (ulong _id in discordMessages.latestMessageID) { await DeleteMessage(_id); }
                    if (isStartingGrid)
                    {
                        foreach (ulong _id in discordMessages.latestStartingGridID) { await DeleteMessage(_id); }
                        discordMessages.latestStartingGridID = new DiscordMessages().latestStartingGridID;
                    }
                    else { discordMessages.latestMessageID = new DiscordMessages().latestMessageID; }
                }
                await SendMessageRecursive(newMessageContent, isStartingGrid, deleteLater);
                discordMessages.WriteJson();
            }
        }

        public static async Task DeleteMessage(ulong messageID)
        {
            IMessage? oldMessage = null;
            if (ChannelEntrylist is not null) { try { oldMessage = await ChannelEntrylist.GetMessageAsync(messageID); } catch { } }
            if (oldMessage is not null) { await oldMessage.DeleteAsync(); }
        }

        public static async Task SendMessageRecursive(string MessageContent, bool isStartingGrid, bool deleteLater = true)
        {
            List<string> keys = new() { "**\n", "\n" };
            string part1;
            string part2;
            if (Settings is not null && MessageContent.Length > Settings.CharLimit)
            {
                int keyNr = 0;
                string key = keys[keyNr];
                int charPos = Settings.CharLimit;
                while (true)
                {
                    charPos--;
                    if (charPos == key.Length && keyNr < keys.Count - 1)
                    {
                        keyNr++;
                        key = keys[keyNr];
                        charPos = Settings.CharLimit;
                    }
                    else if (charPos == key.Length)
                    {
                        part1 = MessageContent.Substring(0, Settings.CharLimit);
                        part2 = MessageContent[Settings.CharLimit..];
                        break;
                    }
                    else if (MessageContent.Substring(charPos - key.Length, key.Length) == key)
                    {
                        part1 = MessageContent[..charPos];
                        part2 = MessageContent[charPos..];
                        break;
                    }
                }
                await SendMessageRecursiveEnding(part1, isStartingGrid);
                await SendMessageRecursive(part2, isStartingGrid);
            }
            else
            {
                await SendMessageRecursiveEnding(MessageContent, isStartingGrid, deleteLater);
            }
        }

        public static async Task SendMessageRecursiveEnding(string MessageContent, bool isStartingGrid, bool deleteLater = true)
        {
            IUserMessage? newMessage = null;
            if (ChannelEntrylist is not null) { newMessage = await ChannelEntrylist.SendMessageAsync(MessageContent); }
            if (newMessage is not null && deleteLater)
            {
                if (isStartingGrid) { discordMessages.latestStartingGridID.Add(newMessage.Id); }
                else { discordMessages.latestMessageID.Add(newMessage.Id); }
            }
        }
    }



    public class DiscordMessages
    {
        public static string Path = MainWindow.dataDirectory + "discordmessages.json";

        public List<ulong> latestMessageID = new() { 0 };
        public List<ulong> latestStartingGridID = new() { 0 };

        public DiscordMessages() { }

        public DiscordMessages(bool isFromBot)
        {
            if (isFromBot)
            {
                if (!File.Exists(Path)) { WriteJson(); }
                ReadJson();
            }
        }

        public void ReadJson()
        {
            try
            {
                DiscordMessages? tempInstance = JsonConvert.DeserializeObject<DiscordMessages>(File.ReadAllText(Path, Encoding.Unicode));
                if (tempInstance?.latestMessageID is IList) { latestMessageID = tempInstance.latestMessageID; }
                if (tempInstance?.latestStartingGridID is IList) { latestStartingGridID = tempInstance.latestStartingGridID; }
            }
            catch { return; }
        }

        public void WriteJson()
        {
            string text = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(Path, text, Encoding.Unicode);
        }
    }



    public class MissingDiscordIDs
    {
        public static string Path = MainWindow.dataDirectory + "discordmissingdiscordids.json";

        public List<int> entryIDs = new();

        public MissingDiscordIDs() { }

        public MissingDiscordIDs(bool isFromBot)
        {
            if (!File.Exists(Path)) { WriteJson(); }
            ReadJson();
        }

        public void ListAdd(int _entryId)
        {
            if (!entryIDs.Contains(_entryId)) { entryIDs.Add(_entryId); WriteJson(); }
        }

        public void ListRemove(int _entryId)
        {
            if (entryIDs.Contains(_entryId)) { entryIDs.Remove(_entryId); WriteJson(); }
        }

        public void ReadJson()
        {
            try
            {
                MissingDiscordIDs? tempInstance = JsonConvert.DeserializeObject<MissingDiscordIDs>(File.ReadAllText(Path, Encoding.Unicode));
                if (tempInstance?.entryIDs is IList) { entryIDs = tempInstance.entryIDs; }
            }
            catch { return; }
        }

        public void WriteJson()
        {
            string text = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(Path, text, Encoding.Unicode);
        }
    }
}
