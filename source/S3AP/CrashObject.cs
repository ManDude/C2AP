using Archipelago.Core.Util;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S3AP
{
    internal class CrashObject
    {
        private static uint listAddress = 0x6CDB0;
        public static uint cacheOffset = 0x80000000; //memory will contain pointers with the 0x8 prefix, but we need to read with 0x0 prefix

        private static uint entityIdOffset = 0xB8;
        public static uint subtypeOffset = 0xB4;
        private static uint childOffset = 0x4C;
        private static uint siblingOffset = 0x48;
        public static uint goolOffset = 0x10;

        private static uint itemOffsetsOffset = 0x10;

        public static uint FindObjectAddress(uint type, uint subtype)
        {
            uint currentListHeader = listAddress;
            uint objAddress = 0;
            while (true)
            {
                Log.Information($"Checking list header at 0x{currentListHeader:X}");
                //Log.Information($"Header value is {Memory.ReadUInt(currentListHeader)}");
                //Log.Information($"Header +0x4 value is {Memory.ReadUInt(currentListHeader + 0x4)}");
                //Log.Information($"Header -0x4 value is {Memory.ReadUInt(currentListHeader - 0x4)}");
                if (Memory.ReadUInt(currentListHeader) != 2)
                {
                    break; //not a valid list
                }
                objAddress = Memory.ReadUInt(currentListHeader + 0x4) - cacheOffset; //first object in list

                objAddress = FindObjectRecursive(objAddress, type, subtype);
                if (objAddress != cacheOffset && objAddress != 0)
                {
                    return objAddress;
                }
                currentListHeader += 0x8; //next list header
            }

            Log.Warning($"Could not find object with type {type}, subtype {subtype} returning");


            return 0;
        }

        private static uint FindObjectRecursive(uint objAddress, uint type, uint subtype)
        {
            Log.Information($"Checking object at address 0x{objAddress:X}");
            if (objAddress == cacheOffset || objAddress == 0) return 0; //check for null pointer

            if (Memory.ReadUInt(objAddress) == 0) //check object header
            {
                Log.Information($"Found a free object in list at 0x{objAddress:X}, skipping...");
                return 0;
            }

            //Log.Information($"Object entity ID is {Memory.ReadUInt(objAddress + entityIdOffset)}");
            Log.Information($"Object subtype is {Memory.ReadUInt(objAddress + subtypeOffset)}");
            if (Memory.ReadUInt(objAddress + subtypeOffset) == subtype)
            {
                Log.Information($"Found object with subtype {subtype} at address 0x{objAddress:X}");

                //check type
                //bool typeMatches = false;
                uint goolEntryAddress = Memory.ReadUInt(objAddress + goolOffset) - cacheOffset;
                if (goolEntryAddress == 0 || goolEntryAddress == cacheOffset)
                {
                    Log.Warning($"Null goolEntry pointer");
                }
                else
                {
                    uint itemAddress = GetItemAddressFromEntry(goolEntryAddress, 0);
                    Log.Information($"Gool entry address is 0x{goolEntryAddress:X}, first item address is 0x{itemAddress:X}");
                    if (Memory.ReadUInt(itemAddress) == type)
                    {
                        Log.Information($"Found object with type {type} at address 0x{objAddress:X}");
                        return objAddress;
                    }
                    Log.Information($"Object type {Memory.ReadUInt(itemAddress)} does not match desired type {type}");
                }
            }

            uint childObjAddress = Memory.ReadUInt(objAddress + childOffset) - cacheOffset; //first child object
            Log.Information($"Recursing into child object at address 0x{childObjAddress:X}");
            uint foundAddress = FindObjectRecursive(childObjAddress, type, subtype);
            if (foundAddress != cacheOffset && foundAddress != 0)
            {
                return foundAddress;
            }
            
            uint siblingObjAddress = Memory.ReadUInt(objAddress + siblingOffset) - cacheOffset; //next sibling object
            Log.Information($"Recursing into sibling object at address 0x{siblingObjAddress:X}");
            return FindObjectRecursive(siblingObjAddress, type, subtype);
            


        }

        public static uint GetItemAddressFromEntry(uint entryAddress, uint itemIndex)
        {
            return Memory.ReadUInt(entryAddress + itemOffsetsOffset + itemIndex*0x4) - cacheOffset;

            //return itemOffset;
        }

        public static uint GetGoolBytecodeAddressFromObject(uint objAddress)
        {
            if (objAddress == cacheOffset || objAddress == 0)
            {
                Log.Warning($"Null object pointer");
                return 0;
            }
            uint goolEntryAddress = Memory.ReadUInt(objAddress + goolOffset) - cacheOffset;
            if (goolEntryAddress == 0 || goolEntryAddress == cacheOffset)
            {
                Log.Warning($"Null goolEntry pointer");
                return 0;
            }
            uint bytecodeAddress = GetItemAddressFromEntry(goolEntryAddress, 1); //second item is bytecode
            Log.Information($"Gool bytecode address is 0x{bytecodeAddress:X}");
            return bytecodeAddress;
        }
    }
}
