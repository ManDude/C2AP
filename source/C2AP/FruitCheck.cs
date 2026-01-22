using Archipelago.Core.Models;
using Archipelago.Core.Util;
using Serilog;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace C2AP
{
    internal class FruitCheck
    {
        private class FruitBundle
        {
            public SortedSet<uint> collectedFruits = new SortedSet<uint>();
            public int requiredFruitCount;
            public int locationId;
        }
        //private struct FruitBundle
        //{
        //    public SortedSet<uint> collectedFruits;
        //    public int requiredFruitCount;
        //    public int locationId;
        //}

        private static Dictionary<uint, int> ?FruitIdToBundle;

        private static List<FruitBundle> ?Bundles;

        private static Timer checkFruitTimer = new Timer();
        public static void Initialize()
        {
            if (FruitIdToBundle == null || Bundles == null)
            {
                try
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    var resourceName = "C2AP.fruitbundles.txt";

                    using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        //if (reader == null) return;
                        FruitIdToBundle = new Dictionary<uint, int>();
                        Bundles = new List<FruitBundle>();

                        string line;
                        uint id = 0;
                        uint levelid = 0;
                        int totalBundles = 0;
                        int currentBundle = -1;
                        FruitBundle bundle = new();
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line[0] == '#') continue;
                            string[] split = line.Split('-');
                            if (split.Length == 1)
                            {
                                levelid = Convert.ToUInt32(split[0], 16);
                                currentBundle = -1;
                            }
                            else
                            {
                                if (Convert.ToInt32(split[0], 16) != currentBundle)
                                {
                                    currentBundle = Convert.ToInt32(split[0], 16);
                                    Bundles.Add(new FruitBundle());
                                    bundle = Bundles.Last();
                                    bundle.locationId = 10000 + totalBundles;
                                    totalBundles++;
                                }
                                id = Convert.ToUInt32(split[1], 16);
                                id = id << 8;
                                id += levelid;
                                FruitIdToBundle[id] = totalBundles-1;
                                bundle.requiredFruitCount++;
                            }
                        }
                    }
                }
                catch (IOException e)
                {
                    Log.Logger.Error($"An error occurred: {e.Message}");
                }
            }
            checkFruitTimer.Interval = 1500; // ms - adjust to desired tick rate
            checkFruitTimer.AutoReset = true;
            checkFruitTimer.Elapsed += (s, ev) =>
            {
                ScanCollectedFruitList();
            };
            checkFruitTimer.Enabled = true;
        }

        public static void ScanCollectedFruitList()
        {
            if (FruitIdToBundle == null) return;
            if (Bundles == null) return;

            uint len = Memory.ReadUInt(Addresses.FruitCollectedListStart);
            if (len == 0) return;

            uint levelId = Memory.ReadByte(Addresses.LevelIdAddress+1);
            //Log.Logger.Information("scanning");
            Memory.ReadByteArray(Addresses.FruitCollectedListStart - len, (int)len);
            //Memory.
            uint id;
            for (uint i = 0; i < len; i += 4)
            {
                //Log.Logger.Information("scanning1");
                id = Memory.ReadUInt(Addresses.FruitCollectedListStart - len + i);
                //id = id << 8;
                id += levelId;
                CheckId(id);
            }

            //clear out the list
            //Log.Logger.Information("clearing");
            Memory.WriteByteArray(Addresses.FruitCollectedListStart - len, new byte[(int)len+4]);

            //check if fruit was added during the scan
            while (true)
            {
                len += 4;
                id = Memory.ReadUInt(Addresses.FruitCollectedListStart - len);
                if (id == 0) break;
                Memory.Write(Addresses.FruitCollectedListStart - len, new byte[4]);
                id += levelId;
                CheckId(id);
            }
        }

        private static void CheckId(uint id)
        {
            if (!FruitIdToBundle.TryGetValue(id, out int value))
            {
                Log.Logger.Warning($"Unknown fruit id: {id:X}");
                return;
            }
            //Log.Logger.Information("scanning3");
            FruitBundle bundle = Bundles[value];
            bundle.collectedFruits.Add(id);

            //bundle.collectedFruits.Add(id);
            //Log.Logger.Information("scanning5");
            //Log.Logger.Information($"bundle #{FruitIdToBundle[id]} added fruit id: {id:X}, collected: {bundle.collectedFruits.ToString()}");
            if (bundle.collectedFruits.Count == bundle.requiredFruitCount)
            {
                App.Client.SendLocation(new Location { Id = bundle.locationId });
                Log.Logger.Information($"sending {bundle.locationId}");
            }
            //Log.Logger.Information("scanning6");
        }

        public static List<uint> DebugScanFruitList()
        {
            List<uint> list = new List<uint>();
            //if (FruitIdToBundle == null) return list;
            //if (Bundles == null) return list;

            uint len = Memory.ReadUInt(Addresses.FruitCollectedListStart);
            if (len == 0) return list;

            uint levelId = Memory.ReadByte(Addresses.LevelIdAddress + 1);
            //Log.Logger.Information("scanning");
            Memory.ReadByteArray(Addresses.FruitCollectedListStart - len, (int)len);
            //Memory.
            uint id;
            for (uint i = 0; i < len; i += 4)
            {
                //Log.Logger.Information("scanning1");
                id = Memory.ReadUInt(Addresses.FruitCollectedListStart - len + i);
                id = id >> 8;
                //id += levelId;
                //CheckId(id);
                list.Add(id);
            }

            //clear out the list
            //Log.Logger.Information("clearing");
            Memory.WriteByteArray(Addresses.FruitCollectedListStart - len, new byte[(int)len + 4]);

            //check if fruit was added during the scan
            while (true)
            {
                len += 4;
                id = Memory.ReadUInt(Addresses.FruitCollectedListStart - len);
                if (id == 0) break;
                Memory.Write(Addresses.FruitCollectedListStart - len, new byte[4]);
                //id += levelId;
                id = id >> 8;
                list.Add(id);
            }
            return list;
        }
    }
}