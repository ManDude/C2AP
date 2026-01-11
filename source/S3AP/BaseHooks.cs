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

            //0x3A8C4 doesn't work
            PauseMenuItems.InsertHook(0x3A8C4, 0xf030);
        }
    }
}
