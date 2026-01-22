using Archipelago.Core;
using Archipelago.Core.AvaloniaGUI.Models;
using Archipelago.Core.AvaloniaGUI.ViewModels;
using Archipelago.Core.AvaloniaGUI.Views;
using Archipelago.Core.GameClients;
using Archipelago.Core.Models;
using Archipelago.Core.Traps;
using Archipelago.Core.Util;
using Archipelago.Core.Util.Hook;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Packets;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.OpenGL;
using Newtonsoft.Json;
using ReactiveUI;
using Serilog;
using SharpDX.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Reflection;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Location = Archipelago.Core.Models.Location;
using Timer = System.Timers.Timer;

namespace C2AP;

public partial class App : Application
{
    public static MainWindowViewModel Context;
    public static ArchipelagoClient Client { get; set; }
    public static List<ILocation> GameLocations { get; set; }
    private static readonly object _lockObject = new object();
    private static Dictionary<string, string> _hintsList { get; set; }
    private static bool _hasSubmittedGoal { get; set; }
    private static bool _useQuietHints { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Start();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Context
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainWindow
            {
                DataContext = Context
            };
        }
        base.OnFrameworkInitializationCompleted();
    }
    
    public void Start()
    {
        Context = new MainWindowViewModel("0.6.2");
        Context.ClientVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();
        Context.ConnectClicked += Context_ConnectClicked;
        Context.CommandReceived += (e, a) =>
        {
            if (string.IsNullOrWhiteSpace(a.Command)) return;
            Client?.SendMessage(a.Command);
            HandleCommand(a.Command);
        };
        Context.ConnectButtonEnabled = true;
        _hintsList = null;
        _hasSubmittedGoal = false;
        _useQuietHints = true;
        //Log.Logger.Information("Hello World");
        Log.Logger.Information("This Archipelago Client is compatible only with the Crash Bandicoot 2 Europe (PAL) Release");
        Log.Logger.Information("Trying to play with a different version will not work and may release all of your locations at the start.");

        //CustomHook blockCircle = new CustomHook([
        //                "la $t0, 0x80069bb8",
        //                "lw $t1, 0($t0)",
        //                "la $t2, 0x20000000",
        //                "or $t1, $t1, $t2",
        //                "sw $t1, 0($t0)",
        //                ]);

        
        
    }


    private void HandleCommand(string command)
    {
        switch (command)
        {
            case "clearSpyroGameState":
                Log.Logger.Information("Clearing the game state.  Please reconnect to the server while in game to refresh received items.");
                Client.ForceReloadAllItems();
                break;
            case "useQuietHints":
                Log.Logger.Information("Hints for found locations will not be displayed.  Type 'useVerboseHints' to show them.");
                _useQuietHints = true;
                break;
            case "useVerboseHints":
                Log.Logger.Information("Hints for found locations will be displayed.  Type 'useQuietHints' to show them.");
                _useQuietHints = false;
                break;
            case "exec":
                //Log.Logger.Information("execing");
                List<uint> objs = CrashObject.FindAllObjectAddresses(3, 16);
                Log.Logger.Information($"objs found:");
                foreach (uint obj in objs)
                {
                    Log.Logger.Information($"obj address: {obj:X}, state: {Memory.ReadUInt(obj + 0x1C):X}, ID: {Memory.ReadUInt(obj + 0xB8):X}, various: {Memory.ReadUInt(obj + 0xD4):X}");
                }
                break;
            
        }
        string[] args = command.Split(' ');
        if (args.Length == 2)
        {
            if (args[0] == "giveloc") //testing crystal locations
            {

                uint address = Addresses.CrystalLocationsAddress;
                int bits = Convert.ToInt32(args[1]);

                address += (uint)(bits / 8);
                bits = bits % 8;
                Memory.WriteBit(address, bits, true);
                Log.Logger.Information($"Checking location at crystal address 0x{address:X}, bit#{bits}");
            }
            if (args[0] == "snapshot")
            {
                string filename = $"memorysnapshot_{args[1]}.mem";
                Log.Logger.Information($"Creating memory snapshot at {filename}");
                if (File.Exists(filename))
                {
                    File.Delete(filename);
                }
                using (FileStream fs = File.Create(filename))
                {
                    byte[] memoryDump = Memory.ReadByteArray(0, 0b1000000000000000000000);
                    fs.Write(memoryDump, 0, memoryDump.Length);
                }
            }
        }
        if (args.Length >= 2) { 
            if (args[0] == "b")
            {
                Log.Logger.Information("bundling");
                string filepath = "bundles.txt";
                int bundleId = -1;
                using (StreamReader reader = new StreamReader(filepath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line[0] == '#') continue;
                        bundleId = Convert.ToInt32(line.Split('-')[0], 16);
                    }
                }
                bundleId++;
                List<uint> fruitList = FruitCheck.DebugScanFruitList();
                string name = "#";
                for (int i = 1; i < args.Length; i++)
                {
                    name += $"{args[i]}";
                    if (i < args.Length - 1)
                    {
                        name += " ";
                    }
                }
                List<string> content = [name];
                foreach (uint fruit in fruitList)
                {
                    content.Add($"{bundleId:X}-{fruit:X}");
                }
                try
                {
                    File.AppendAllLines(filepath, content);
                    Log.Logger.Information("Content appended successfully.");
                }
                catch (Exception ex)
                {
                    Log.Logger.Information($"An error occurred: {ex.Message}");
                }
                Log.Logger.Information("bundled");
            }
        }
    }
    private async void Context_ConnectClicked(object? sender, ConnectClickedEventArgs e)
    {
        
        if (Client != null)
        {
            Client.CancelMonitors();
            Client.Connected -= OnConnected;
            Client.Disconnected -= OnDisconnected;
            Client.ItemReceived -= ItemReceived;
            Client.MessageReceived -= Client_MessageReceived;
            Client.LocationCompleted -= Client_LocationCompleted;
            Client.CurrentSession.Locations.CheckedLocationsUpdated -= Locations_CheckedLocationsUpdated;
        }
        DuckstationClient? client = null;
        try
        {
            client = new DuckstationClient();
        }
        catch (ArgumentException ex)
        {
            Log.Logger.Warning("Duckstation not running, open Duckstation and launch the game before connecting!");
            return;
        }
        var DuckstationConnected = client.Connect();
        if (!DuckstationConnected)
        {
            Log.Logger.Warning("Duckstation not running, open Duckstation and launch the game before connecting!");
            return;
        }
        Client = new ArchipelagoClient(client);
        Client.ShouldSaveStateOnItemReceived = false;

        Memory.GlobalOffset = Memory.GetDuckstationOffset();

        //InputLock.Initialize();
        //InputLock.LockInput(InputFlag.Square);
        BaseHooks.Initialize();

        //FruitCheck.Initialize();

        Client.Connected += OnConnected;
        Client.Disconnected += OnDisconnected;

        await Client.Connect(e.Host, "Crash2");
        if (!Client.IsConnected)
        {
            Log.Logger.Error("Your host seems to be invalid.  Please confirm that you have entered it correctly.");
            return;
        }
        GameLocations = Helpers.BuildLocationList();
        Client.LocationCompleted += Client_LocationCompleted;
        Client.CurrentSession.Locations.CheckedLocationsUpdated += Locations_CheckedLocationsUpdated;
        Client.MessageReceived += Client_MessageReceived;
        Client.ItemReceived += ItemReceived;
        Client.EnableLocationsCondition = () => Helpers.IsInGame();
        await Client.Login(e.Slot, !string.IsNullOrWhiteSpace(e.Password) ? e.Password : null);
        //if (Client.Options?.Count > 0)
        //{
        //    Client.MonitorLocations(GameLocations);
        //    Log.Logger.Information("Warnings and errors above are okay if this is your first time connecting to this multiworld server.");
        //}
        //else
        //{
        //    Log.Logger.Error("Failed to login.  Please check your host, name, and password.");
        //}
        Client.MonitorLocations(GameLocations);
        CrashObjectMod.Initialize();
    }

    private void UpdateGemLocationsChecked()
    {
        Log.Debug("UpdateGemLocationsChecked");
        byte[] gemFlags = Memory.ReadByteArray(Addresses.GemLocationsAddress, 8);
        for (int i = 0; i < gemFlags.Length; i++)
            Log.Debug($"gemflags {i}: {gemFlags[i]:X}");
        gemFlags[Addresses.ColoredGemOffset] &= Addresses.ColoredGemMaskNegated; //clear out colored gem bits
        for (int i = 0; i < gemFlags.Length; i++)
            Log.Debug($"gemflags {i}: {gemFlags[i]:X}");
        byte receivedColoredGemFlags = Memory.ReadByte(Addresses.ColoredGemReceivedAddress);
        Log.Debug($"receivedColoredGemFlags: {receivedColoredGemFlags:X}");
        receivedColoredGemFlags &= Addresses.ColoredGemMask; //clear out clear gem bits
        Log.Debug($"receivedColoredGemFlags: {receivedColoredGemFlags:X}");

        gemFlags[Addresses.ColoredGemOffset] |= receivedColoredGemFlags; //set colored gem bits from received items
        for (int i = 0; i < gemFlags.Length; i++)
            Log.Debug($"gemflags {i}: {gemFlags[i]:X}");
        Memory.WriteByteArray(Addresses.GemLocationsWithReceivedColoredGemsAddress, gemFlags);
        SyncGameState();
    }

    private void Client_LocationCompleted(object? sender, LocationCompletedEventArgs e)
    {
        if (Client.GameState == null) return;
        //UpdateGemLocationsChecked();
        //var currentEggs = CalculateCurrentEggs();
        SyncGameState();
        CheckGoalCondition();
    }

    public static void SyncGameState()
    {
        if (Client.GameState == null) return;

        Log.Debug($"syncing");
        // Convert ILocation list to Location list if needed
        List<Location> locations = Client.GameState.CompletedLocations.OfType<Location>().ToList();
        foreach (Location location in locations)
        {
            Log.Debug($"address: {location.Address:X}, bit: {location.AddressBit}");
            Memory.WriteBit(location.Address, location.AddressBit, true);

            if (location.Address != Addresses.GemLocationsAddress + Addresses.ColoredGemOffset || ((0x1 << location.AddressBit) & Addresses.ColoredGemMask) == 0)
            {
                uint offset = (uint) location.Address - Addresses.GemLocationsAddress;
                Memory.WriteBit(Addresses.GemLocationsWithReceivedColoredGemsAddress + offset, location.AddressBit, true);
            }
        }
        List<Item> items = Client.GameState.ReceivedItems;
        uint crystalCount = 0;
        uint clearGemCount = 0;
        List<int> coloredGems = new();
        foreach (Item item in items)
        {
            switch (item.Name)
            {
                case "Crystal":
                    crystalCount++;
                    break;
                case "Clear Gem":
                    clearGemCount++;
                    break;
                case "Red Gem":
                    coloredGems.Add(Addresses.RedGemReceivedBit);
                    break;
                case "Green Gem":
                    coloredGems.Add(Addresses.GreenGemReceivedBit);
                    break;
                case "Purple Gem":
                    coloredGems.Add(Addresses.PurpleGemReceivedBit);
                    break;
                case "Blue Gem":
                    coloredGems.Add(Addresses.BlueGemReceivedBit);
                    break;
                case "Yellow Gem":
                    coloredGems.Add(Addresses.YellowGemReceivedBit);
                    break;
            }
        }
        //update center lift with current crystalCount
        if (CrashObjectMod.liftMod == null)
        {
            Log.Debug("Lift mod is not initialized!");
        }
        else
        {
            List<byte[]> mods = new();
            mods.Add(CustomHook.ConvertAsm([$"addiu $a0, $zero, 0x{crystalCount:X}"]).ToArray());
            mods.Add(CustomHook.ConvertAsm([$"addiu $v1, $zero, 0x{crystalCount:X}"]).ToArray());

            List<uint> modInstructionLines = [6507 - CrashObjectMod.magicOffset / 4, 6507];
            CrashObjectMod.liftMod.EditMod(mods, modInstructionLines);
        }
        

        //set crystal item flags
        byte[] bytes = new byte[8];
        for (int i = 0; i < bytes.Length; i++)
        {
            for (int j = 1; j < 0xFF; j = j << 1)
            {
                if (crystalCount == 0) break;
                crystalCount--;
                bytes[i] |= (byte) j;
            }
            if (crystalCount == 0) break;
        }
        Memory.WriteByteArray(Addresses.CrystalsReceivedAddress, bytes);

        //set clear gem item flags
        bytes = new byte[8];
        for (int i = 0; i < bytes.Length; i++)
        {
            int bit = 1;
            for (int j = 0; j < 8; j++)
            {
                if (clearGemCount == 0) break;
                clearGemCount--;
                if (i == Addresses.ColoredGemOffset && j == Addresses.RedGemReceivedBit)
                {
                    j = Addresses.YellowGemReceivedBit + 0x1;
                }
                bytes[i] |= (byte)bit;
                bit = bit << 1;
            }
            if (clearGemCount == 0) break;
        }
        Memory.WriteByteArray(Addresses.GemsReceivedAddress, bytes);

        //set colored gem flags
        foreach (int coloredGemBit in coloredGems)
        {
            Memory.WriteBit(Addresses.ColoredGemReceivedAddress, coloredGemBit, true);
            Memory.WriteBit(Addresses.GemLocationsWithReceivedColoredGemsAddress + Addresses.ColoredGemOffset, coloredGemBit, true);
        }
        Log.Debug($"done syncing");
    }
    private async void ItemReceived(object? o, ItemReceivedEventArgs args)
    {
        Log.Logger.Debug($"Item Received: {JsonConvert.SerializeObject(args.Item)}");
        uint crashAddress;
        switch (args.Item.Name)
        {
            case "Life":
                crashAddress = CrashObject.FindObjectAddress(0, 0);
                if (crashAddress != 0 && crashAddress != CrashObject.cacheOffset)
                {
                    IncrementByte(crashAddress + Addresses.LivesOffset);
                }
                IncrementByte(Addresses.LivesGlobalAddress);
                break;
            case "Wumpa Fruit":
                crashAddress = CrashObject.FindObjectAddress(0, 0);
                if (crashAddress != 0 && crashAddress != CrashObject.cacheOffset)
                {
                    IncrementByte(crashAddress + Addresses.WumpaOffset);
                }
                IncrementByte(Addresses.WumpaGlobalAddress);
                break;
            default:
                SyncGameState();
                break;
        }
    }

    private static void IncrementByte(uint address)
    {
        uint data = Memory.ReadByte(address);
        data++;
        if (data > 0xFF) 
            data = 0xFF;
        Memory.WriteByte(address, (byte) data);
    }

    private static void CheckGoalCondition()
    {
        if (_hasSubmittedGoal)
        {
            return;
        }
        byte levelid = Memory.ReadByte(Addresses.LevelIdAddress + 0x1);
        if (levelid == 0x29 || levelid == 0x28)
        {
            Client.SendGoalCompletion();
            _hasSubmittedGoal = true;
        }
    }
    private static async void RunLagTrap()
    {
        using (var lagTrap = new LagTrap(TimeSpan.FromSeconds(20)))
        {
            lagTrap.Start();
            await lagTrap.WaitForCompletionAsync();
        }
    }
    
    private static void LogItem(Item item)
    {
        // Not supported at this time.
        /*var messageToLog = new LogListItem(new List<TextSpan>()
            {
                new TextSpan(){Text = $"[{item.Id.ToString()}] -", TextColor = new SolidColorBrush(Color.FromRgb(255, 255, 255))},
                new TextSpan(){Text = $"{item.Name}", TextColor = new SolidColorBrush(Color.FromRgb(200, 255, 200))}
            });
        lock (_lockObject)
        {
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                Context.ItemList.Add(messageToLog);
            });
        }*/
    }

    private void Client_MessageReceived(object? sender, MessageReceivedEventArgs e)
    {
        // If the player requests it, don't show "found" hints in the main client.
        if (e.Message.Parts.Any(x => x.Text == "[Hint]: ") && (!_useQuietHints || !e.Message.Parts.Any(x => x.Text.Trim() == "(found)")))
        {
            LogHint(e.Message);
        }
        if (!e.Message.Parts.Any(x => x.Text == "[Hint]: ") || !_useQuietHints || !e.Message.Parts.Any(x => x.Text.Trim() == "(found)"))
        {
            Log.Logger.Information(JsonConvert.SerializeObject(e.Message));
        }
    }
    private static void LogHint(LogMessage message)
    {
        var newMessage = message.Parts.Select(x => x.Text);

        foreach (var hint in Context.HintList)
        {
            IEnumerable<string> hintText = hint.TextSpans.Select(y => y.Text);
            if (newMessage.Count() != hintText.Count())
            {
                continue;
            }
            bool isMatch = true;
            for (int i = 0; i < hintText.Count(); i++)
            {
                if (newMessage.ElementAt(i) != hintText.ElementAt(i))
                {
                    isMatch = false;
                    break;
                }
            }
            if (isMatch)
            {
                return; //Hint already in list
            }
        }
        List<TextSpan> spans = new List<TextSpan>();
        foreach (var part in message.Parts)
        {
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                spans.Add(new TextSpan() { Text = part.Text, TextColor = new SolidColorBrush(Color.FromRgb(part.Color.R, part.Color.G, part.Color.B)) });
            });
        }
        lock (_lockObject)
        {
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                Context.HintList.Add(new LogListItem(spans));
            });
        }
    }
    private static void Locations_CheckedLocationsUpdated(System.Collections.ObjectModel.ReadOnlyCollection<long> newCheckedLocations)
    {
        if (Client.GameState == null) return;
        CheckGoalCondition();

    }
    
    private static void OnConnected(object sender, EventArgs args)
    {
        Log.Logger.Information("Connected to Archipelago");
        Log.Logger.Information($"Playing {Client.CurrentSession.ConnectionInfo.Game} as {Client.CurrentSession.Players.GetPlayerName(Client.CurrentSession.ConnectionInfo.Slot)}");

        // There is a tradeoff here when creating new threads.  Separate timers allow for better control over when
        // memory reads and writes will happen, but they take away threads for other client tasks.
        // This solution is fine with the current item pool size but won't scale with gemsanity.
        // TODO: Test which of these can be combined without impacting the end result.

        //_loadGameTimer = new Timer();
        //_loadGameTimer.Elapsed += new ElapsedEventHandler(StartSpyroGame);
        //_loadGameTimer.Interval = 5000;
        //_loadGameTimer.Enabled = true;

        
        // Repopulate hint list.  There is likely a better way to do this using the Get network protocol
        // with keys=[$"hints_{team}_{slot}"].
        Client?.SendMessage("!hint");
        SyncGameState();
    }

    private static void OnDisconnected(object sender, EventArgs args)
    {
        Log.Logger.Information("Disconnected from Archipelago");
        // Avoid ongoing timers affecting a new game.
        _hintsList = null;
        _hasSubmittedGoal = false;
        _useQuietHints = true;
        Log.Logger.Information("This Archipelago Client is compatible only with the Crash Bandicoot 2 Europe (PAL) Release");
        Log.Logger.Information("Trying to play with a different version will not work and may release all of your locations at the start.");

       
    }
}
