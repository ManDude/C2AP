using System.Collections.Generic;

namespace S3AP
{
    public static class Addresses
    {
        public const uint CrystalsReceivedAddress = 0x0000EA00; //64 bits
        public const uint GemsReceivedAddress = 0x0000EA10; //64 bits

        public const uint ColoredGemReceivedAddress = 0x0000EA17;
        public const int RedGemReceivedBit = 2;
        public const int GreenGemReceivedBit = 3;
        public const int PurpleGemReceivedBit = 4;
        public const int BlueGemReceivedBit = 5;
        public const int YellowGemReceivedBit = 6;

        public const uint GemLocationsWithReceivedColoredGemsAddress = 0x0000EA20; //64 bits
        public const uint ColoredGemOffset = 0x7;
        public const byte ColoredGemMask =        0b01111100;
        public const byte ColoredGemMaskNegated = 0b10000011;

        public const uint CrystalLocationsAddress = 0x0006D03C; //64 bits

        //public const uint WasSwappedAddress = 0x0000EA1C; //32 bit boolean
        //public const uint CrystalLocationsSwapAddress = 0x0000EA20; // 64 bits

        public const uint GemLocationsAddress = 0x0006CEC0;

        public const uint LevelIdAddress = 0x6ce08; //32 bits (hopefully)

        //crash
        public const uint LivesOffset = 0x145;

        public static Dictionary<string, int> BitOfLocation = new Dictionary<string, int>
        {
            //crystals
            {"Sewer or Later Crystal", 10 },
            {"Night Fight Crystal", 12 },
            {"Hangin' Out Crystal", 13 },
            {"Snow Go Crystal", 14 },
            {"Ruination Crystal", 15 },
            {"Piston it Away Crystal", 16 },
            {"Snow Biz Crystal", 17 },
            {"Rock It Crystal", 18 },
            {"Cold Hard Crash Crystal",  19},
            {"Diggin' It Crystal", 21 },
            {"Road to Ruin Crystal", 22 },
            {"Un-Bearable Crystal", 23 },
            {"Crash Dash Crystal", 24 },
            {"Hang Eight Crystal", 25 },
            {"Pack Attack Crystal", 26  },
            {"Crash Crush Crystal", 27 },
            {"Bear It Crystal", 29 },
            {"Turtle Woods Crystal", 30 },
            {"The Pits Crystal", 31  },
            {"Air Crash Crystal", 32 },
            {"Plant Food Crystal", 33 },
            {"Bear Down Crystal", 34  },
            {"The Eel Deal Crystal", 35 },
            {"Bee-Having Crystal", 36 },
            {"Spaced Out Crystal", 38 },

            //gems
            {"Hang Eight Clear Gem (Timer)", 1 },
            {"Air Crash Clear Gem (Death Route)", 2 },
            {"Sewer or Later Clear Gem (Yellow Gem Path)", 3 },
            {"Road to Ruin Clear Gem (Death Route)", 4 },
            {"Piston it Away Clear Gem (Death Route)", 5 },
            {"Night Fight Clear Gem (Death Route)", 6 },
            {"Spaced Out Clear Gem (All Colored Gems Path)", 7 },
            {"Diggin' It Clear Gem (Death Route)", 8 },
            {"Cold Hard Crash Clear Gem (Death Route)", 9 },
            {"Sewer or Later Clear Gem (Box Gem)", 10 },
            {"Night Fight Clear Gem (Box Gem)", 12 },
            {"Hangin' Out Clear Gem (Box Gem)", 13 },
            {"Snow Go Clear Gem (Box Gem)", 14 },
            {"Ruination Clear Gem (Box Gem)", 15 },
            {"Piston it Away Clear Gem (Box Gem)", 16 },
            {"Snow Biz Clear Gem (Box Gem)", 17 },
            {"Rock It Clear Gem (Box Gem)", 18 },
            {"Cold Hard Crash Clear Gem (Box Gem)", 19 },
            {"Diggin' It Clear Gem (Box Gem)", 21 },
            {"Road to Ruin Clear Gem (Box Gem)", 22 },
            {"Un-Bearable Clear Gem (Box Gem)", 23 },
            {"Crash Dash Clear Gem (Box Gem)", 24 },
            {"Hang Eight Clear Gem (Box Gem)", 25 },
            {"Pack Attack Clear Gem (Box Gem)", 26 },
            {"Crash Crush Clear Gem (Box Gem)", 27 },
            {"Bear It Clear Gem (Box Gem)", 29 },
            {"Turtle Woods Clear Gem (Box Gem)", 30 },
            {"The Pits Clear Gem (Box Gem)", 31 },

            {"Air Crash Clear Gem (Box Gem)", 48 },
            {"Plant Food Clear Gem (Box Gem)", 49 },
            {"Bear Down Clear Gem (Box Gem)", 50 },
            {"The Eel Deal Clear Gem (Box Gem)", 51 },
            {"Bee-Having Clear Gem (Box Gem)", 52 },
            {"Totally Bear Clear Gem (Box Gem)", 53 },
            {"Spaced Out Clear Gem (Box Gem)", 54 },
            {"Totally Fly Clear Gem (Box Gem)", 55 },
            {"Ruination Clear Gem (Green Gem Path)", 57 },
            {"Snow Go Red Gem", 58 },
            {"The Eel Deal Green Gem", 59 },
            {"Bee-Having Purple Gem", 60 },
            {"Turtle Woods Blue Gem", 61 },
            {"Plant Food Yellow Gem", 62 },
        };
        // warp 1: 30, 14, 25, 31, 24 

        public static Dictionary<string, int> LocationIdInApWorld = new Dictionary<string, int> //copy pasted from AP world
        {
            { "Turtle Woods Crystal", 1 },
            { "Turtle Woods Clear Gem (Box Gem)", 2 },
            { "Turtle Woods Blue Gem", 3 },

            { "Snow Go Crystal", 4 },
            { "Snow Go Clear Gem (Box Gem)", 5 },
            { "Snow Go Red Gem", 6 },

            { "Hang Eight Crystal", 7 },
            { "Hang Eight Clear Gem (Box Gem)", 8 },
            { "Hang Eight Clear Gem (Timer)", 9 },

            { "The Pits Crystal", 10 },
            { "The Pits Clear Gem (Box Gem)", 11 },

            { "Crash Dash Crystal", 12 },
            { "Crash Dash Clear Gem (Box Gem)", 13 },

            { "Ripper Roo Defeated", 14 },

            { "Snow Biz Crystal", 15 },
            { "Snow Biz Clear Gem (Box Gem)", 16 },

            { "Air Crash Crystal", 17 },
            { "Air Crash Clear Gem (Box Gem)", 18 },
            { "Air Crash Clear Gem (Death Route)", 19 },

            { "Bear It Crystal", 20 },
            { "Bear It Clear Gem (Box Gem)", 21 },

            { "Crash Crush Crystal", 22 },
            { "Crash Crush Clear Gem (Box Gem)", 23 },

            { "The Eel Deal Crystal", 24 },
            { "The Eel Deal Clear Gem (Box Gem)", 25 },
            { "The Eel Deal Green Gem", 26 },

            { "Komodo Brothers Defeated", 27 },

            { "Plant Food Crystal", 28 },
            { "Plant Food Clear Gem (Box Gem)", 29 },
            { "Plant Food Yellow Gem", 30 },

            { "Sewer or Later Crystal", 31 },
            { "Sewer or Later Clear Gem (Box Gem)", 32 },
            { "Sewer or Later Clear Gem (Yellow Gem Path)", 33 },

            { "Bear Down Crystal", 34 },
            { "Bear Down Clear Gem (Box Gem)", 35 },

            { "Road to Ruin Crystal", 36 },
            { "Road to Ruin Clear Gem (Box Gem)", 37 },
            { "Road to Ruin Clear Gem (Death Route)", 38 },

            { "Un-Bearable Crystal", 39 },
            { "Un-Bearable Clear Gem (Box Gem)", 40 },

            { "Tiny Tiger Defeated", 41 },

            { "Hangin' Out Crystal", 42 },
            { "Hangin' Out Clear Gem (Box Gem)", 43 },

            { "Diggin' It Crystal", 44 },
            { "Diggin' It Clear Gem (Box Gem)", 45 },
            { "Diggin' It Clear Gem (Death Route)", 46 },

            { "Cold Hard Crash Crystal", 47 },
            { "Cold Hard Crash Clear Gem (Box Gem)", 48 },
            { "Cold Hard Crash Clear Gem (Death Route)", 49 },

            { "Ruination Crystal", 50 },
            { "Ruination Clear Gem (Box Gem)", 51 },
            { "Ruination Clear Gem (Green Gem Path)", 52 },

            { "Bee-Having Crystal", 53 },
            { "Bee-Having Clear Gem (Box Gem)", 54 },
            { "Bee-Having Purple Gem", 55 },

            { "Dr. N. Gin Defeated", 56 },

            { "Piston it Away Crystal", 57 },
            { "Piston it Away Clear Gem (Box Gem)", 58 },
            { "Piston it Away Clear Gem (Death Route)", 59 },

            { "Rock It Crystal", 60 },
            { "Rock It Clear Gem (Box Gem)", 61 },

            { "Night Fight Crystal", 62 },
            { "Night Fight Clear Gem (Box Gem)", 63 },
            { "Night Fight Clear Gem (Death Route)", 64 },

            { "Pack Attack Crystal", 65 },
            { "Pack Attack Clear Gem (Box Gem)", 66 },

            { "Spaced Out Crystal", 67 },
            { "Spaced Out Clear Gem (Box Gem)", 68 },
            { "Spaced Out Clear Gem (All Colored Gems Path)", 69 },

            // { "Dr. Neo Cortex Defeated", 70 },

            { "Totally Bear Clear Gem (Box Gem)", 71 },
            { "Totally Fly Clear Gem (Box Gem)", 72 },
        };


    }

    
}
