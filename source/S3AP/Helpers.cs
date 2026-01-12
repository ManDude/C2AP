using Archipelago.Core.Models;
using Archipelago.Core.Util;
using S3AP.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using static S3AP.Models.Enums;
using Location = Archipelago.Core.Models.Location;
namespace S3AP
{
    public class Helpers
    {
        private static GameStatus lastNonZeroStatus = GameStatus.Spawning;
        public static string OpenEmbeddedResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string jsonFile = reader.ReadToEnd();
                return jsonFile;
            }
        }
        
        public static bool IsInGame()
        {
            return true;
        }
        
        public static List<ILocation> BuildLocationList()
        {
            //int id = 10000;
            List<ILocation> locations = new List<ILocation>();
            uint address;
            int bit;
            foreach (string locName in Addresses.BitOfLocation.Keys)
            {
                Location loc;
                if (locName.Contains("Gem"))
                {
                    address = Addresses.GemLocationsAddress;
                    bit = Addresses.BitOfLocation[locName];

                    address += (uint)(bit / 8);
                    bit = bit % 8;

                    //if (!Addresses.LocationIdInApWorld.ContainsKey(locName))
                    //{
                    //    Log.Logger.Warning($"Location ID for {locName} not found, skipping...");
                    //    continue;
                    //}

                    //if (locName.Equals("Turtle Woods Blue Gem"))
                    //{
                    //    //debug
                    //    Log.Logger.Information($"Turtle Woods Blue Gem location address 0x{address:X}, bit#{bit}");
                    //}

                    loc = new Location
                    {
                        Name = locName,
                        Address = address,
                        AddressBit = bit,
                        CheckType = LocationCheckType.Bit,
                        Category = "Gem",
                        Id = Addresses.LocationIdInApWorld[locName],
                    };
                }
                else
                {
                    address = Addresses.CrystalLocationsAddress;
                    bit = Addresses.BitOfLocation[locName];

                    address += (uint)(bit / 8);
                    bit = bit % 8;

                    //if (locName.Equals("Turtle Woods Crystal"))
                    //{
                    //    //debug
                    //    Log.Logger.Information($"Turtle Woods Crystal location address 0x{address:X}, bit#{bit}");
                    //}

                    loc = new Location
                    {
                        Name = locName,
                        Address = address,
                        AddressBit = bit,
                        CheckType = LocationCheckType.Bit,
                        Category = "Crystal",
                        Id = Addresses.LocationIdInApWorld[locName],
                    };
                }
                    
                locations.Add(loc);
            }
            
            return locations;
        }

        
    }
}
