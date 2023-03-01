using Discord.Commands;
using Discord.WebSocket;
using System.Linq;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Discord;
using GTRCLeagueManager.Database;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Collections;


namespace GTRCLeagueManager
{
    public class SignInOutBot
    {
        public static DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;
        public static readonly string token = "MTAwNDc5NTMxNjQ2MzIyNzA0MA.G4Qg1w.-_7ccWcVoZrun6jx-k_7KreF-1fE-blNNhrJzc"; //Azubi
        //public static readonly string token = "MTAwODQwMDUyMzM5MDYzNjE4NA.GuiMFH.L0A38VZ9n1enIUMCyAn5-HTqVlLl99XzsqFLW0"; //Mitarbeiter
        public static readonly long BotDiscordID = 1004795316463227040; //Azubi
        //public static readonly long BotDiscordID = 1008400523390636184; //Mitarbeiter
        public static readonly ulong adminRoleID = 1008463611619983470; //Azubi
        //public static readonly ulong adminRoleID = 818631385379242004; //Mitarbeiter
        public static readonly ulong ServerID = 669254433305001995; //Azubi
        //public static readonly ulong ServerID = 818621398259335210; //Mitarbeiter
        public static readonly ulong channelID = 1008453364448768070; //Azubi
        //public static readonly ulong channelID = 1008452986713931816; //Mitarbeiter Bojack
        //public static readonly ulong channelID = 999415406529884282; //Mitarbeiter
        public static readonly int charLimit = 2000;

        public async Task RunBotAsync()
        {
            _client = new DiscordSocketClient();
            _commands = new CommandService();
            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .BuildServiceProvider();
            _client.Log += _client_Log;
            await RegisterCommandsAsync();
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
            await Task.Delay(-1);
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
            List<string> Tags = new List<string>();
            List<string> TagList = TagDiscordID(BotDiscordID);
            foreach (var _tag in TagList) { Tags.Add(_tag + " "); }
            Tags.Add("!");
            foreach (string tag in Tags)
            {
                if (UserMessage.HasStringPrefix(tag, ref argPos))
                {
                    var result = await _commands.ExecuteAsync(context, argPos, _services);
                    if (!result.IsSuccess)
                    {
                        //Commands.Channel = UserMessage.Channel;
                        //Commands.Instance.IsAdmin = RaceControl.Statics.ExistsUniqueProp(Driver.Statics.GetByUniqueProp((long)UserMessage.Author.Id, 1).ID);
                        //await Commands.Instance.Help();
                        await UserMessage.DeleteAsync();
                    }
                }
            }
        }

        public static List<string> TagDiscordID(long _discordID)
        {
            List<string> TagList = new List<string>();
            TagList.Add("<@" + _discordID.ToString() + ">");
            TagList.Add("<@!" + _discordID.ToString() + ">");
            return TagList;
        }
    }

    /*
    public class Commands : ModuleBase<SocketCommandContext>
    {
        public static Commands Instance;
        public static PreSeasonVM iPreSVM = PreSeasonVM.Instance;
        public static DiscordMessages discordMessages = new DiscordMessages(true);
        public static Emoji emojiSuccess = new Emoji("✅");
        public static Emoji emojiFail = new Emoji("❌");
        public static Emoji emojiWTF = new Emoji("🤷‍♀️");
        public static Emoji emojiSleep = new Emoji("😴");
        public static Emoji emojiShocked = new Emoji("😱");
        public static Emoji emojiRaceCar = new Emoji("🏎️");
        public static Emoji emojiPartyFace = new Emoji("🥳");
        public static Emoji emojiCry = new Emoji("😭");
        public static Emoji emojiThinking = new Emoji("🤔");
        public static string adminRoleTag = "<@&" + SignInOutBot.adminRoleID.ToString() + ">";
        public static ISocketMessageChannel Channel = SignInOutBot._client?.GetGuild(SignInOutBot.ServerID)?.GetTextChannel(SignInOutBot.channelID);

        public string LogText = "";
        public SocketUserMessage UserMessage = null;
        public bool RegisterType = true;
        public bool IsAdmin = false;
        public long DiscordID_Author = Basics.NoID;
        public long DiscordID_Driver = Basics.NoID;
        public List<long> DiscordIDs_Drivers = new List<long>();
        public string strDiscordID_Driver = Basics.NoID.ToString();
        public int CarNr = Basics.NoID;
        public int EventNr = -1;
        public int RaceNumber = Basics.NoID;

        public Commands() { Instance = this; }



        [Command("abmelden")]
        public async Task SignOutCmd()
        {
            SetDefaultProperties();
            RegisterType = false;
            await ParseEventNr((Event.GetEventByCurrentDate().EventNr + 1).ToString());
            await SignInOut();
        }

        [Command("anmelden")]
        public async Task SignInCmd()
        {
            SetDefaultProperties();
            RegisterType = true;
            await ParseEventNr((Event.GetEventByCurrentDate().EventNr + 1).ToString());
            await SignInOut();
        }

        [Command("abmelden")]
        public async Task SignOutCmd(string strEventNr)
        {
            SetDefaultProperties();
            RegisterType = false;
            await ParseEventNr(strEventNr);
            await SignInOut();
        }

        [Command("anmelden")]
        public async Task SignInCmd(string strEventNr)
        {
            SetDefaultProperties();
            RegisterType = true;
            await ParseEventNr(strEventNr);
            await SignInOut();
        }

        [Command("abmelden")]
        public async Task SignOutCmd(string strEventNr, string strDiscordID)
        {
            SetDefaultProperties();
            RegisterType = false;
            await ParseEventNr(strEventNr); await ParseDiscordID(strDiscordID);
            await SignInOut();
        }

        [Command("anmelden")]
        public async Task SignInCmd(string strEventNr, string strDiscordID)
        {
            SetDefaultProperties();
            RegisterType = true;
            await ParseEventNr(strEventNr); await ParseDiscordID(strDiscordID);
            await SignInOut();
        }

        [Command("zurückziehen")]
        public async Task PullOutCmd()
        {
            SetDefaultProperties();
            RegisterType = false;
            await ParseEventNr((Event.GetEventByCurrentDate().EventNr + 1).ToString());
            await PullInOut();
        }

        [Command("zurückziehen")]
        public async Task PullOutCmd(string strDiscordID)
        {
            SetDefaultProperties();
            RegisterType = false;
            await ParseEventNr((Event.GetEventByCurrentDate().EventNr + 1).ToString());
            await ParseDiscordID(strDiscordID);
            await PullInOut();
        }

        [Command("dochnichtzurückziehen")]
        public async Task UndoPullOutCmd(string strDiscordID)
        {
            SetDefaultProperties();
            RegisterType = true;
            await ParseEventNr((Event.GetEventByCurrentDate().EventNr + 1).ToString());
            await ParseDiscordID(strDiscordID);
            await PullInOut();
        }

        [Command("fahrzeugwechsel")]
        public async Task ChangeCarCmd(string strCarNr)
        {
            SetDefaultProperties();
            await ParseEventNr((Event.GetEventByCurrentDate().EventNr + 1).ToString());
            await ParseCarNr(strCarNr);
            await ChangeCar();
        }

        [Command("fahrzeugwechsel")]
        public async Task ChangeCarCmd(string strCarNr, string strEventNr)
        {
            SetDefaultProperties();
            await ParseCarNr(strCarNr); await ParseEventNr(strEventNr);
            await ChangeCar();
        }

        [Command("fahrzeugwechsel")]
        public async Task ChangeCarCmd(string strCarNr, string strEventNr, string strDiscordID)
        {
            SetDefaultProperties();
            await ParseCarNr(strCarNr); await ParseEventNr(strEventNr); await ParseDiscordID(strDiscordID);
            await ChangeCar();
        }

        [Command("kalender")]
        public async Task ShowEventsCmd()
        {
            SetDefaultProperties();
            await ShowEvents();
            await UserMessage.DeleteAsync();
        }

        [Command("fahrzeugliste")]
        public async Task ShowCarsCmd()
        {
            SetDefaultProperties();
            await ParseEventNr((Event.GetEventByCurrentDate().EventNr + 1).ToString());
            await ShowCars();
            await UserMessage.DeleteAsync();
        }

        [Command("bop")]
        public async Task ShowBoPCmd()
        {
            SetDefaultProperties();
            await ParseEventNr((Event.GetEventByCurrentDate().EventNr + 1).ToString());
            await ShowBoP();
            await UserMessage.DeleteAsync();
        }

        [Command("bop")]
        public async Task ShowBoPCmd(string strEventNr)
        {
            SetDefaultProperties();
            await ParseEventNr(strEventNr);
            await ShowBoP();
            await UserMessage.DeleteAsync();
        }

        [Command("starterfeld")]
        public async Task ShowStarterfeldCmd()
        {
            SetDefaultProperties();
            await ParseEventNr((Event.GetEventByCurrentDate().EventNr + 1).ToString());
            await ShowStartingGrid(false, false);
            await UserMessage.DeleteAsync();
        }

        [Command("starterfeld")]
        public async Task ShowStarterfeldCmd(string strEventNr)
        {
            SetDefaultProperties();
            await ParseEventNr(strEventNr);
            await ShowStartingGrid(false, false);
            await UserMessage.DeleteAsync();
        }



        public async Task ErrorResponse()
        {
            await ReplyAsync(LogText);
            await UserMessage.AddReactionAsync(emojiFail);
        }

        public void SetDefaultProperties()
        {
            Channel = Context.Channel;
            UserMessage = Context.Message;
            DiscordID_Author = (long)Context.Message.Author.Id;
            DiscordID_Driver = (long)Context.Message.Author.Id;
            IsAdmin = RaceControl.Statics.ExistsUniqueProp(Driver.Statics.GetByUniqueProp((long)UserMessage.Author.Id, 1).ID);
        }

        public async Task ParseCarNr(string strCarNr)
        {
            LogText = "Bitte eine gültige Fahrzeugnummer angeben.";
            if (Int32.TryParse(strCarNr, out int intCarNr))
            {
                if (Car.Statics.ExistsUniqueProp(intCarNr)) { await ErrorResponse(); await ShowCars(); }
                else { CarNr = intCarNr; }
            }
            else { await ErrorResponse(); }
        }

        public async Task ParseEventNr(string strEventNr)
        {
            if (Int32.TryParse(strEventNr, out int intEventNr))
            {
                LogText = "Bitte eine Event-Nr zwischen 1 und " + (Event.List.Count).ToString() + " angeben.";
                if (intEventNr < 1) { await ErrorResponse(); await ShowEvents(); }
                else if (intEventNr > Event.List.Count) { await ErrorResponse(); await ShowEvents(); }
                else { EventNr = intEventNr - 1; }
            }
            else { LogText = "Bitte eine gültige Event-Nr angeben."; await ErrorResponse(); }
        }

        public async Task ParseDiscordID(string strDiscordID)
        {
            if (Int64.TryParse(strDiscordID, out long longDiscordID_Driver)) { DiscordID_Driver = longDiscordID_Driver; }
            else { LogText = "Bitte eine gültige Discord-ID oder Startnummer angeben."; await ErrorResponse(); }
        }

        public async Task SetRacenumber()
        {
            int _raceNumber;
            if (Driver.IsValidDiscordID(DiscordID_Driver))
            {
                int _driverID = Driver.Statics.GetByUniqueProp(DiscordID_Driver, 1).ID;
                long _steamID = Driver.Statics.GetByUniqueProp(DiscordID_Driver, 1).SteamID;
                if (DiscordID_Driver == SignInOutBot.BotDiscordID)
                {
                    await UserMessage.AddReactionAsync(emojiRaceCar);
                    if (RegisterType) { await UserMessage.AddReactionAsync(emojiPartyFace); }
                    else { await UserMessage.AddReactionAsync(emojiCry); }
                }
                else if (_driverID != Basics.NoID)
                {
                    _raceNumber = Entry.Statics.GetByUniqueProp(Entry.Statics.GetByUniqueProp(DriverEntries.Statics.GetByUniqueProp(_steamID).EntryID).RaceNumber).RaceNumber;
                    if (_raceNumber != Entry.NoID) { RaceNumber = _raceNumber; SetDiscordIDs_Drivers(); }
                    else
                    {
                        if (DiscordID_Driver == DiscordID_Author) { LogText = "Du bist noch nicht für die Meisterschaft registriert. Falls du dich gerade erst angemeldet hast, versuche es doch bitte in " + PreSeasonVM.Instance.EntriesUpdateRemTime + " erneut. " + adminRoleTag + " schaut euch das Problem bitte an."; await ErrorResponse(); }
                        else { LogText = "Der Fahrer ist nicht für die Meisterschaft registriert. Die Datenbank wird das nächste Mal in " + PreSeasonVM.Instance.EntriesUpdateRemTime + " synchronisiert."; await ErrorResponse(); }
                    }
                }
                else
                {
                    if (DiscordID_Driver == DiscordID_Author) { LogText = "Du bist nicht in der Datenbank eingetragen. Vermutlich haben die " + adminRoleTag + " nur vergessen, deine Discord-ID zu speichern."; await ErrorResponse(); }
                    else { LogText = "Der Fahrer ist nicht in der Datenbank eingetragen. " + adminRoleTag + "Möglicherweise fehlt seine Discord-ID."; await ErrorResponse(); }
                }
            }
            else if (Int32.TryParse(DiscordID_Driver.ToString(), out _raceNumber))
            {
                if (Entry.getEntryByRaceNumber(_raceNumber).RaceNumber != Entry.NoID) { RaceNumber = _raceNumber; CheckAuthorInEntry(); SetDiscordIDs_Drivers(); }
                else { LogText = "Die Startnummer " + _raceNumber.ToString() + " ist noch nicht für die Meisterschaft registriert. Falls du dich gerade erst angemeldet hast, versuche es doch bitte in " + PreSeasonVM.Instance.EntriesUpdateRemTime + " erneut. " + adminRoleTag + " schaut euch das Problem bitte an."; await ErrorResponse(); }
            }
            else { LogText = "Bitte eine gültige Discord-ID oder Startnummer angeben."; await ErrorResponse(); }
        }

        public void CheckAuthorInEntry()
        {
            List<Driver> _drivers = DriverEntries.GetDriversByRaceNumber(RaceNumber);
            foreach (Driver _driver in _drivers) { if (_driver.DiscordID == DiscordID_Author) { DiscordID_Driver = DiscordID_Author; break; } }
        }

        public void SetDiscordIDs_Drivers()
        {
            List<Driver> _drivers = DriverEntries.GetDriversByRaceNumber(RaceNumber);
            foreach (Driver _driver in _drivers) { if (_driver.ID != Basics.NoID) { DiscordIDs_Drivers.Add(_driver.DiscordID); } }
        }

        public string TagDiscordIDs(List<long> listDiscordIDs, bool mobileType)
        {
            string tagText= "";
            foreach (long _discordID in listDiscordIDs) { tagText += TagDiscordID(_discordID, mobileType); }
            return tagText;
        }

        public string TagDiscordID(long _discordID, bool mobileType)
        {
            string tagText = "";
            if (mobileType) { tagText += "<@!" + _discordID.ToString() + ">"; }
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
            text += "`!fahrzeugwechsel 10` | Zum nächsten Event auf das Auto 10 wechseln\n";
            text += "`!fahrzeugwechsel 10 4` | Zum 4. Event auf das Auto 10 wechseln\n\n";
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
                text += "`!fahrzeugwechsel 10 4 612` | Zum 4. Event StartNr/DiscordID 612 auf das Auto 10 umschreiben";
            }
            text += "\n\nStatt das `!` zu verwenden kannst du mich auch direkt mit `@` ansprechen.";
            await SendMessage(text, false);
        }

        public async Task ChangeCar()
        {
            await SetRacenumber();
            if (RaceNumber != Entry.NoID && CarNr != Basics.NoID && EventNr > -1)
            {
                if (DiscordID_Author == DiscordID_Driver || IsAdmin)
                {
                    Event.SortByDate();
                    Event _event = Event.List[EventNr];
                    EventsEntries _eventsEntries = EventsEntries.GetEventsEntriesByEDRN(RaceNumber, _event.EventDate);
                    Entry _entry = Entry.getEntryByRaceNumber(RaceNumber);
                    if (_entry.SignOutDate < DateTime.Now)
                    {
                        if (DiscordID_Author == DiscordID_Driver) { LogText = "Du hast dich aus der Meisterschaft zurückgezogen."; }
                        else { LogText = "Der Teilnehmer hat sich aus der Meisterschaft zurückgezogen."; }
                        await ErrorResponse();
                    }
                    else if (_event.EventDate < DateTime.Now)
                    {
                        await ReplyAsync("Das Rennen ist doch schon vorbei.");
                        await UserMessage.AddReactionAsync(emojiThinking);
                    }
                    else if (_eventsEntries.CarID == CarNr)
                    {
                        if (DiscordID_Author == DiscordID_Driver) { await ReplyAsync("Du bist doch schon auf dem " + Car.Statics.GetByUniqueProp(CarNr).Name + " angemeldet."); }
                        else { await ReplyAsync("Der Teilnehmer ist bereits auf dem " + Car.Statics.GetByUniqueProp(CarNr).Name + " angemeldet."); }
                        await UserMessage.AddReactionAsync(emojiThinking);
                    }
                    else if (iPreSVM.CarChangeCount(RaceNumber, _event.EventDate) >= iPreSVM.CarChangeLimit)
                    {
                        if (DiscordID_Author == DiscordID_Driver) { LogText = "Du kannst dein Fahrzeug nicht mehr wechseln."; }
                        else { LogText = "Dieser Teilnehmer kann das Fahrzeug nicht mehr wechseln."; }
                        await ErrorResponse();
                    }
                    else
                    {
                        iPreSVM.UpdateBoPForEvent(_event.EventDate);
                        CarBoP _carBoP = CarBoP.GetCarByCarID(_entry.CarID);
                        if (_entry.ScorePoints)
                        {
                            for (int index = EventNr; index < Event.List.Count; index++)
                            {
                                EventsEntries _tempEventsEntries = EventsEntries.GetEventsEntriesByEDRN(RaceNumber, Event.List[index].EventDate);
                                _tempEventsEntries.CarID = CarNr;
                                _tempEventsEntries.CarChangeDate = DateTime.Now;
                            }
                        }
                        else { _eventsEntries.CarID = CarNr; }
                        iPreSVM.UpdateBoPForEvent(_event.EventDate);
                        await ShowStartingGrid(true, true);
                        await UserMessage.AddReactionAsync(emojiSuccess);
                        if (_entry.ScorePoints && !_eventsEntries.ScorePoints)
                        {
                            await ReplyAsync(TagDiscordIDs(DiscordIDs_Drivers, false) + "Seit dem " + Event.EventDate2String(iPreSVM.DateCarChangeLimit, "DD.MM.YY") +
                                " um " + Event.EventDate2String(iPreSVM.DateCarChangeLimit, "hh:mm") +
                                " Uhr kann nicht mehr als Stammfahrer auf dieses Fahrzeug gewechselt werden, da die Obergrenze von " +
                                iPreSVM.CarLimitRegisterLimit.ToString() + " Fahrzeugen erreicht wurde. Daher fährst du in dieser Meisterschaft mit dem " +
                                Car.Statics.GetByUniqueProp(CarNr).Name + " außerhalb der Wertung als Gaststarter mit, bis dieses Fahrzeug wieder weniger als " +
                                iPreSVM.CarLimitRegisterLimit.ToString() +
                                "x in der Meisterschaft vertreten ist. Solltest du auch beim nächsten Rennen gerne als Stammfahrer teilnehmen wollen, wähle bitte ein anderes Fahrzeug aus.");
                        }
                    }
                }
                else
                {
                    LogText = "Netter Versuch, aber du kannst nur deine eigene Fahrzeugwahl ändern :stuck_out_tongue:";
                    await ErrorResponse();
                }
            }
        }

        public async Task SignInOut()
        {
            await SetRacenumber();
            if (RaceNumber != Entry.NoID && EventNr > -1)
            {
                if (DiscordID_Author == DiscordID_Driver || IsAdmin)
                {
                    Event.SortByDate();
                    Event _event = Event.List[EventNr];
                    EventsEntries _eventsEntries = EventsEntries.GetEventsEntriesByEDRN(RaceNumber, _event.EventDate);
                    Entry entry = Entry.getEntryByRaceNumber(RaceNumber);
                    if (entry.SignOutDate < DateTime.Now)
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
                    else if (_event.EventDate <= DateTime.Now)
                    {
                        LogText = "Leider zu spät. Die Deadline war am " + Event.EventDate2String(_event.EventDate, "DD.MM.YY") + " um " + Event.EventDate2String(_event.EventDate, "hh:mm") + " Uhr.";
                        await ErrorResponse();
                    }
                    else
                    {
                        if (RegisterType) { _eventsEntries.SignInDate = DateTime.Now; }
                        else { _eventsEntries.SignInDate = Event.DateTimeMaxValue; }
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
        }

        public async Task PullInOut()
        {
            await SetRacenumber();
            if (RaceNumber != Entry.NoID)
            {
                Entry entry = Entry.getEntryByRaceNumber(RaceNumber);
                bool currentRegisterState = entry.SignOutDate > DateTime.Now; if (RegisterType) { currentRegisterState = entry.SignOutDate >= Event.DateTimeMaxValue; }
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
                    if (RegisterType) { entry.SignOutDate = Event.DateTimeMaxValue; }
                    else { entry.SignOutDate = DateTime.Now; }
                    await ShowStartingGrid(false, false);
                    await UserMessage.AddReactionAsync(emojiSuccess);
                    Entry.Statics.WriteSQL();
                }
            }
        }

        public async Task ShowEvents()
        {
            string text = "**Kalender**\n";
            foreach (Event _event in Event.List)
            {
                text += (_event.EventNr + 1).ToString() + ".\t";
                text += Event.EventDate2String(_event.EventDate, "DD.MM.\t");
                text += Track.Statics.GetByUniqueProp(_event.TrackID).Name + "\n";
            }
            await SendMessage(text, false);
        }

        public async Task ShowCars()
        {
            if (EventNr > -1)
            {
                var tempReply = await ReplyAsync(":sleeping:");
                Event _event = Event.List[EventNr];
                iPreSVM.UpdateBoPForEvent(_event.EventDate);
                string text = "**Fahrzeugliste**\n";
                var linqList = from _car in Car.Statics.List
                               orderby _car.Name
                               select _car;
                List<Car> _carList = linqList.Cast<Car>().ToList();
                foreach (Car _car in _carList)
                {
                    CarBoP _carBoP = CarBoP.GetCarByCarID(_car.AccCarID);
                    if (_car.Category == "GT3" && (_car.IsLatestVersion || IsAdmin))
                    {
                        text += _car.AccCarID.ToString() + "\t";
                        text += _car.Name;
                        text += " (" + _car.Year.ToString() + ") - ";
                        text += _carBoP.CountBoP.ToString() + "/" + iPreSVM.CarLimitRegisterLimit.ToString() + "\n";
                    }
                }
                await SendMessage(text, false);
                await tempReply.DeleteAsync();
            }
        }

        public async Task ShowBoP()
        {
            if (EventNr > -1)
            {
                var tempReply = await ReplyAsync(":sleeping:");
                Event _event = Event.List[EventNr];
                iPreSVM.UpdateBoPForEvent(_event.EventDate);
                string text = "**BoP für Event " + _event.Name + Event.EventDate2String(_event.EventDate, " (DD.MM.YY)**\n");
                foreach (CarBoP _carBoP in CarBoP.List)
                {
                    if (_carBoP.Car.Category == "GT3" && _carBoP.CountBoP > 0)
                    {
                        text += _carBoP.CountBoP.ToString() + "x\t";
                        text += _carBoP.Ballast.ToString() + " kg\t";
                        //text += _carBoP.Restrictor.ToString() + "%\t";
                        text += _carBoP.Car.Name + "\n";
                    }
                }
                await SendMessage(text, false);
                await tempReply.DeleteAsync();
            }
        }

        public async Task ShowStartingGrid(bool printCar, bool printCarChange)
        {
            if (EventNr > -1)
            {
                var tempReply = await ReplyAsync(":sleeping:");
                Event.SortByDate();
                Event _event = Event.List[EventNr];
                await CreateStartingGridMessage(RaceNumber, _event.EventDate, printCar, printCarChange);
                await tempReply.DeleteAsync();
            }
        }

        public static async Task CreateStartingGridMessage(int _raceNumber, DateTime _eventDate, bool printCar, bool printCarChange)
        {
            Event _event = Event.getEventByDate(_eventDate);
            EventsEntries _eventsEntries = EventsEntries.GetEventsEntriesByEDRN(_raceNumber, _event.EventDate);
            int SlotsTaken = iPreSVM.ThreadExportEntrylist(_event.EventDate);
            //if (iPreSVM.IsCheckedRegisterLimit && iPreSVM.DateRegisterLimit < DateTime.Now) { printCarChange = true; }

            List<Entry> EntriesSortRaceNumber = new List<Entry>();
            List<Entry> EntriesSortPreQualiPos = new List<Entry>();
            List<Entry> EntriesSortSignInDate = new List<Entry>();
            List<Entry> NoEntriesSortRaceNumber = new List<Entry>();
            PreQualiResultLine.LoadSQL();
            foreach (Entry _entry in Entry.Statics.List)
            {
                if (_entry.RegisterDate < _event.EventDate && _entry.SignOutDate > _event.EventDate) { EntriesSortRaceNumber.Add(_entry); }
                else { NoEntriesSortRaceNumber.Add(_entry); }
            }
            var linqList = from _entry in EntriesSortRaceNumber
                           orderby EventsEntries.GetEventsEntriesByEDRN(_entry.RaceNumber, _event.EventDate).ScorePoints descending, _entry.RaceNumber
                           select _entry;
            EntriesSortRaceNumber = linqList.Cast<Entry>().ToList();
            linqList = from _entry in EntriesSortRaceNumber
                       orderby PreQualiResultLine.List.IndexOf(PreQualiResultLine.getLineByRaceNumber(_entry.RaceNumber))
                       select _entry;
            EntriesSortPreQualiPos = linqList.Cast<Entry>().ToList();
            linqList = from _entry in EntriesSortRaceNumber
                       orderby EventsEntries.GetEventsEntriesByEDRN(_entry.RaceNumber, _event.EventDate).SignInDate
                       select _entry;
            EntriesSortSignInDate = linqList.Cast<Entry>().ToList();
            linqList = from _entry in NoEntriesSortRaceNumber
                       orderby EventsEntries.GetEventsEntriesByEDRN(_entry.RaceNumber, _event.EventDate).ScorePoints descending, _entry.RaceNumber
                       select _entry;
            NoEntriesSortRaceNumber = linqList.Cast<Entry>().ToList();

            string text = "**Starterfeld für Event " + _event.Name + Event.EventDate2String(_event.EventDate, " (DD.MM.YY)") + " | "
                + SlotsTaken.ToString() + "/" + iPreSVM.GetSlotsAvalable(Track.Statics.GetByUniqueProp(_event.TrackID)).ToString() + "**\n";
            string textTemp = "";
            int pos = 1;
            foreach (Entry entry in EntriesSortRaceNumber)
            {
                _eventsEntries = EventsEntries.GetEventsEntriesByEDRN(entry.RaceNumber, _event.EventDate);
                if (_eventsEntries.SignInState && _eventsEntries.IsOnEntrylist)
                {
                    (textTemp, pos) = AddStartingGridLine(textTemp, pos, entry, _eventsEntries, printCar, printCarChange, false);
                }
            }
            if (pos > 1) { text += textTemp; } else { text += "-\n"; }

            textTemp = "";
            pos = 1;
            foreach (Entry entry in EntriesSortPreQualiPos)
            {
                _eventsEntries = EventsEntries.GetEventsEntriesByEDRN(entry.RaceNumber, _event.EventDate);
                bool candidate = _eventsEntries.SignInState && !_eventsEntries.IsOnEntrylist;
                if (candidate && entry.ScorePoints && entry.RegisterDate < iPreSVM.DateRegisterLimit)
                {
                    (textTemp, pos) = AddStartingGridLine(textTemp, pos, entry, _eventsEntries, printCar, printCarChange, true);
                }
            }
            foreach (Entry entry in EntriesSortPreQualiPos)
            {
                _eventsEntries = EventsEntries.GetEventsEntriesByEDRN(entry.RaceNumber, _event.EventDate);
                bool candidate = _eventsEntries.SignInState && !_eventsEntries.IsOnEntrylist;
                if (candidate && entry.ScorePoints && entry.RegisterDate >= iPreSVM.DateRegisterLimit)
                {
                    (textTemp, pos) = AddStartingGridLine(textTemp, pos, entry, _eventsEntries, printCar, printCarChange, true);
                }
            }
            foreach (Entry entry in EntriesSortSignInDate)
            {
                _eventsEntries = EventsEntries.GetEventsEntriesByEDRN(entry.RaceNumber, _event.EventDate);
                bool candidate = _eventsEntries.SignInState && !_eventsEntries.IsOnEntrylist;
                if (candidate && !entry.ScorePoints)
                {
                    (textTemp, pos) = AddStartingGridLine(textTemp, pos, entry, _eventsEntries, printCar, printCarChange, true);
                }
            }
            if (pos > 1) { text += "\n**Warteliste (" + (pos - 1).ToString() + ")**\n" + textTemp; }

            textTemp = "";
            pos = 1;
            foreach (Entry entry in EntriesSortRaceNumber)
            {
                _eventsEntries = EventsEntries.GetEventsEntriesByEDRN(entry.RaceNumber, _event.EventDate);
                if (!_eventsEntries.SignInState)
                {
                    (textTemp, pos) = AddStartingGridLine(textTemp, pos, entry, _eventsEntries, printCar, printCarChange, false);
                }
            }
            if (pos > 1) { text += "\n**Abmeldungen (" + (pos - 1).ToString() + ")**\n" + textTemp; }

            textTemp = "";
            pos = 1;
            foreach (Entry entry in NoEntriesSortRaceNumber)
            {
                _eventsEntries = EventsEntries.GetEventsEntriesByEDRN(entry.RaceNumber, _event.EventDate);
                (textTemp, pos) = AddStartingGridLine(textTemp, pos, entry, _eventsEntries, printCar, printCarChange, false);
            }
            if (pos > 1) { text += "\n**Aus der Meisterschaft zurückgezogen (" + (pos - 1).ToString() + ")**\n" + textTemp; }

            await SendMessage(text, true);
        }

        public static (string, int) AddStartingGridLine(string text, int pos, Entry entry, EventsEntries _eventsEntries, bool printCar, bool printCarChange, bool printPos)
        {
            if (printPos) { text += pos.ToString() + ".\t"; }
            text += entry.RaceNumber.ToString() + "\t";
            text += DriverEntries.GetDriverStringByRaceNumber(entry.RaceNumber, "FullName");
            if (!_eventsEntries.ScorePoints) { if (entry.ScorePoints) { text += " (Gaststarter | wg. Fzglimit)"; } else { text += " (Gaststarter)"; } }
            if (printCar) { text += "\t" + Car.Statics.GetByUniqueProp(_eventsEntries.CarID).Name; }
            if (printCarChange && entry.ScorePoints)
            {
                text += "\t(" + iPreSVM.CarChangeCount(_eventsEntries.EntryID, Event.DateTimeMaxValue) + "/" + iPreSVM.CarChangeLimit + ")";
            }
            text += "\n"; pos++;
            return (text, pos);
        }

        public static async Task SendMessage(string newMessageContent, bool isStartingGrid)
        {
            
            if (Channel == null) { Channel = SignInOutBot._client?.GetGuild(SignInOutBot.ServerID)?.GetTextChannel(SignInOutBot.channelID); }
            if (Channel != null)
            {
                foreach (ulong _id in discordMessages.latestMessageID) { await DeleteMessage(_id); }
                if (isStartingGrid)
                {
                    foreach (ulong _id in discordMessages.latestStartingGridID) { await DeleteMessage(_id); }
                    discordMessages.latestStartingGridID = new DiscordMessages().latestStartingGridID;
                }
                else { discordMessages.latestMessageID = new DiscordMessages().latestMessageID; }
                await SendMessageRecursive(newMessageContent, isStartingGrid);
                discordMessages.WriteJson();
            }
        }

        public static async Task DeleteMessage(ulong messageID)
        {
            IMessage oldMessage = null;
            try { oldMessage = await Channel.GetMessageAsync(messageID); } catch { }
            if (oldMessage != null) { await oldMessage.DeleteAsync(); }
        }

        public static async Task SendMessageRecursive(string MessageContent, bool isStartingGrid)
        {
            List<string> keys = new List<string> { "**\n", "\n" };
            string part1;
            string part2;
            if (MessageContent.Length > SignInOutBot.charLimit)
            {
                int keyNr = 0;
                string key = keys[keyNr];
                int charPos = SignInOutBot.charLimit;
                while (true)
                {
                    charPos--;
                    if (charPos == key.Length && keyNr < keys.Count - 1)
                    {
                        keyNr++;
                        key = keys[keyNr];
                        charPos = SignInOutBot.charLimit;
                    }
                    else if (charPos == key.Length)
                    {
                        part1 = MessageContent.Substring(0, SignInOutBot.charLimit);
                        part2 = MessageContent.Substring(SignInOutBot.charLimit, MessageContent.Length - SignInOutBot.charLimit);
                        break;
                    }
                    else if (MessageContent.Substring(charPos - key.Length, key.Length) == key)
                    {
                        part1 = MessageContent.Substring(0, charPos);
                        part2 = MessageContent.Substring(charPos, MessageContent.Length - charPos);
                        break;
                    }
                }
                await SendMessageRecursiveEnding(part1, isStartingGrid);
                await SendMessageRecursive(part2, isStartingGrid);
            }
            else
            {
                await SendMessageRecursiveEnding(MessageContent, isStartingGrid);
            }
        }

        public static async Task SendMessageRecursiveEnding(string MessageContent, bool isStartingGrid)
        {
            IUserMessage newMessage;
            newMessage = await Channel.SendMessageAsync(MessageContent);
            if (isStartingGrid) { discordMessages.latestStartingGridID.Add(newMessage.Id); }
            else { discordMessages.latestMessageID.Add(newMessage.Id); }
        }
    }



    public class DiscordMessages
    {
        public static string Path = MainWindow.dataDirectory + "discordmessages.json";

        public List<ulong> latestMessageID = new List<ulong>() { 0 };
        public List<ulong> latestStartingGridID = new List<ulong>() { 0 };

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
                DiscordMessages tempInstance = JsonConvert.DeserializeObject<DiscordMessages>(File.ReadAllText(Path, Encoding.Unicode));
                if (tempInstance.latestMessageID is IList) { latestMessageID = tempInstance.latestMessageID; }
                if (tempInstance.latestStartingGridID is IList) { latestStartingGridID = tempInstance.latestStartingGridID; }
            }
            catch { return; }
        }

        public void WriteJson()
        {
            string text = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(Path, text, Encoding.Unicode);
        }
    }
    */
}
