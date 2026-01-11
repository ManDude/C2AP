using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S3AP
{
    internal class BaseHooks
    {
        private static CustomHook? PauseMenuItems;

        public static void Initialize()
        {
            uint crystalAddressDelta = 0x0006d03c - Addresses.CrystalsReceivedAddress;
            PauseMenuItems = new CustomHook([
                "addiu $sp, $sp, 0xFFF0",
                "sw $t0, 0x4($sp)",
                "sw $t1, 0x8($sp)",
                "la $t0, 0x8005f418", //address of "paused"
                "lw $t1, 0($t0)",
                "nop", //load delay
                "beq $t1 $zero, 0x8", //"bne $t0 $zero, 0x8", //"beq $t1 $zero, 0x8", //branch to exit
                "la $t0, 0x8006d03c",
                "beq $v0 $t0, 0x2", //branch to eval
                "addiu $t0, $t0, 0x4",
                "bne $v0 $t0, 0x3", //branch to exit
                //eval
                $"la $t1, 0x{crystalAddressDelta:X}",
                "subu $v0, $v0, $t1",
                //exit
                "lw $t0, 0x4($sp)",
                "lw $t1, 0x8($sp)",
                "addiu $sp, $sp, 0x10",
            ]);
            //uint offset = 0x80000000;

            //PauseMenuItems = new CustomHook([
            //    "addiu $sp, $sp, 0xFFF0",
            //    "sw $t0, 0x4($sp)",
            //    "sw $t1, 0x8($sp)",
            //    "sw $t2, 0xC($sp)",

            //    "la $t0, 0x8005f418", //address of "paused"
            //    "lw $t0, 0($t0)",
            //    $"la $t2, 0x{Addresses.WasSwappedAddress + offset:X}",
            //    "lw $t1, 0($t2)",

            //    "beq $t0, $t1, 0x1D", //branch to exit
            //    $"la $t1, 0x{Addresses.CrystalLocationsAddress + offset:X}", //t1 holds the game's crystals address
            //    "sw $t0, 0($t2)", //set was swapped to paused state
            //    "beq $t0, $zero, 0x12", //branch to unpause
            //    //pause
            //    //save locations, overwrite with crystal items
            //    $"la $t0, 0x{Addresses.CrystalLocationsSwapAddress + offset:X}",
            //    "lw $t2, 0($t1)",
            //    "sw $t2, 0($t0)",
            //    "addiu $t0, $t0, 0x4",
            //    "addiu $t1, $t1, 0x4",
            //    "lw $t2, 0($t1)",
            //    "sw $t2, 0($t0)",

            //    $"la $t0, 0x{Addresses.CrystalsReceivedAddress + offset:X}",
            //    "addiu $t1, $t1, 0xFFFC", //subtract the 4 added earlier
            //    "lw $t2, 0($t0)",
            //    "sw $t2, 0($t1)",
            //    "addiu $t0, $t0, 0x4",
            //    "addiu $t1, $t1, 0x4",
            //    "lw $t2, 0($t0)",
            //    "sw $t2, 0($t1)",
            //    "beq $zero, $zero, 0x8", //branch to exit

            //    //unpause
            //    //restore locations
            //    $"la $t0, 0x{Addresses.CrystalLocationsSwapAddress + offset:X}",
            //    "lw $t2, 0($t0)",
            //    "sw $t2, 0($t1)",
            //    "addiu $t0, $t0, 0x4",
            //    "addiu $t1, $t1, 0x4",
            //    "lw $t2, 0($t0)",
            //    "sw $t2, 0($t1)",


            //    //exit
            //    "lw $t0, 0x4($sp)",
            //    "lw $t1, 0x8($sp)",
            //    "lw $t2, 0xC($sp)",
            //    "addiu $sp, $sp, 0x10",
            //    ]);

            //0x3A8C4 doesn't work
            PauseMenuItems.InsertHook(0x3A8C0, 0xf030);
            //PauseMenuItems.InsertHook(0x4A8E0, 0xf030);
        }
    }
}
