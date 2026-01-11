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
        
        public static List<ILocation> BuildLocationList(bool includeGems = true, bool includeSkillPoints = true)
        {
            
            List<ILocation> locations = new List<ILocation>();
            
            return locations;
        }

        
    }
}
