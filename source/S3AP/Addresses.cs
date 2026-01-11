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
        
        public const uint CrystalLocationsAddress = 0x0006D03C; //64 bits

        public const uint WasSwappedAddress = 0x0000EA1C; //32 bit boolean
        public const uint CrystalLocationsSwapAddress = 0x0000EA20; // 64 bits
    }
}
