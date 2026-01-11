using Archipelago.Core;
using Archipelago.Core.AvaloniaGUI.Models;
using Archipelago.Core.AvaloniaGUI.ViewModels;
using Archipelago.Core.AvaloniaGUI.Views;
using Archipelago.Core.GameClients;
using Archipelago.Core.Models;
using Archipelago.Core.Traps;
using Archipelago.Core.Util;
using Archipelago.Core.Util.Hook;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Packets;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.OpenGL;
using Newtonsoft.Json;
using ReactiveUI;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reflection;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using static S3AP.Models.Enums;
using Timer = System.Timers.Timer;

namespace S3AP;

public partial class App : Application
{
    public static MainWindowViewModel Context;
    public static ArchipelagoClient Client { get; set; }
    public static List<ILocation> GameLocations { get; set; }
    private static readonly object _lockObject = new object();
    private static Queue<string> _cosmeticEffects { get; set; }
    private static Dictionary<string, string> _hintsList { get; set; }
    private static byte _sparxUpgrades { get; set; }
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
        _sparxUpgrades = 0;
        _hasSubmittedGoal = false;
        _useQuietHints = true;
        Log.Logger.Information("Hello World");
        Log.Logger.Information("This Archipelago Client is compatible only with the NTSC-U 1.1 release of Spyro 3 (North America Greatest Hits version).");
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

        

        Client.Connected += OnConnected;
        Client.Disconnected += OnDisconnected;

        await Client.Connect(e.Host, "Crash2");
        if (!Client.IsConnected)
        {
            Log.Logger.Error("Your host seems to be invalid.  Please confirm that you have entered it correctly.");
            return;
        }
        GameLocations = Helpers.BuildLocationList();
        _cosmeticEffects = new Queue<string>();
        Client.LocationCompleted += Client_LocationCompleted;
        Client.CurrentSession.Locations.CheckedLocationsUpdated += Locations_CheckedLocationsUpdated;
        Client.MessageReceived += Client_MessageReceived;
        Client.ItemReceived += ItemReceived;
        Client.EnableLocationsCondition = () => Helpers.IsInGame();
        await Client.Login(e.Slot, !string.IsNullOrWhiteSpace(e.Password) ? e.Password : null);
        if (Client.Options?.Count > 0)
        {
            // Client.MonitorLocations(GameLocations);
            Log.Logger.Information("Warnings and errors above are okay if this is your first time connecting to this multiworld server.");
        }
        else
        {
            Log.Logger.Error("Failed to login.  Please check your host, name, and password.");
        }
    }

    private void Client_LocationCompleted(object? sender, LocationCompletedEventArgs e)
    {
        if (Client.GameState == null) return;
        //var currentEggs = CalculateCurrentEggs();
        CheckGoalCondition();
    }

    private async void ItemReceived(object? o, ItemReceivedEventArgs args)
    {
        Log.Logger.Debug($"Item Received: {JsonConvert.SerializeObject(args.Item)}");
        //int currentHealth;

        // when receiving a gem/crystal, just set a bit to 1 in a set part in free memory, the specific crystal/white gem doesn't matter, only the count matters
        uint bytenum = 0;
        int bitnum = 0;
        switch (args.Item.Name)
        {
            case "Crystal":
                while (Memory.ReadBit(Addresses.CrystalsReceivedAddress + bytenum, bitnum))
                {
                    bitnum++;
                    if (bitnum > 7)
                    {
                        bitnum = 0;
                        bytenum++;
                    }
                }
                if (bytenum > 8)
                {
                    Log.Logger.Warning("Received crystals above the 64 crystal limit (somehow)");
                    return;
                } 
                Memory.WriteBit(Addresses.CrystalsReceivedAddress + bytenum, bitnum, true);
                break;
            case "Clear Gem":
                while (Memory.ReadBit(Addresses.GemsReceivedAddress + bytenum, bitnum))
                {
                    bitnum++;
                    if (bitnum > 7)
                    {
                        bitnum = 0;
                        bytenum++;
                    }
                    if (bytenum == 7 && bitnum == 2)
                    {
                        //skip over the colored gems
                        bitnum = 7;
                    }
                }
                if (bytenum > 8)
                {
                    Log.Logger.Warning("Received gems above the 64 gems limit (somehow)");
                    return;
                }
                Memory.WriteBit(Addresses.GemsReceivedAddress + bytenum, bitnum, true);
                break;
            case "Red Gem":
                Memory.WriteBit(Addresses.ColoredGemReceivedAddress, Addresses.RedGemReceivedBit, true);
                break;
            case "Green Gem":
                Memory.WriteBit(Addresses.ColoredGemReceivedAddress, Addresses.GreenGemReceivedBit, true);
                break;
            case "Purple Gem":
                Memory.WriteBit(Addresses.ColoredGemReceivedAddress, Addresses.PurpleGemReceivedBit, true);
                break;
            case "Blue Gem":
                Memory.WriteBit(Addresses.ColoredGemReceivedAddress, Addresses.BlueGemReceivedBit, true);
                break;
            case "Yellow Gem":
                Memory.WriteBit(Addresses.ColoredGemReceivedAddress, Addresses.YellowGemReceivedBit, true);
                break;
            case "Life":
                Log.Logger.Information("Recieving lives is not yet implemented in this Archipelago Client.");
                break;
        }
        
    }

    

    private static void CheckGoalCondition()
    {
        if (_hasSubmittedGoal)
        {
            return;
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
    }

    private static void OnDisconnected(object sender, EventArgs args)
    {
        Log.Logger.Information("Disconnected from Archipelago");
        // Avoid ongoing timers affecting a new game.
        _hintsList = null;
        _sparxUpgrades = 0;
        _hasSubmittedGoal = false;
        _useQuietHints = true;
        Log.Logger.Information("This Archipelago Client is compatible only with the NTSC-U 1.1 release of Spyro 3 (North America Greatest Hits version).");
        Log.Logger.Information("Trying to play with a different version will not work and may release all of your locations at the start.");

        //if (_loadGameTimer != null)
        //{
        //    _loadGameTimer.Enabled = false;
        //    _loadGameTimer = null;
        //}
        
       
    }
}
