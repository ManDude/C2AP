using Archipelago.Core.Util;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S3AP
{
    internal class CustomHook
    {
        public List<string> _asm;

        private List<byte> _bytes;

        private ulong _targetAddress;

        private int _targetInstructionSize;

        private ulong _freeAddress;

        private ulong _hookSize;
        public CustomHook(List<string> asm) { 
            _asm = asm;
            _bytes = ConvertAsm(asm);
            _targetAddress = 0;
            _targetInstructionSize = 0;
            _freeAddress = 0;
        }


        public static List<byte> ConvertAsm(List<string> asm)
        {
            List<byte> bytes = new List<byte>();
            for (int i = 0; i < asm.Count; i++)
            {
                byte[] instructionBytes = new byte[4];
                string[] instruction = asm[i].Split([' ', ',']);
                switch(instruction[0])
                {
                    case "jmp":
                        {
                            instructionBytes[0] = 0x08;
                            if (instruction.Length != 2)
                            {
                                Log.Error($"CustomHook: Invalid jmp instruction format at line {i + 1}");
                                break;
                            }
                            instruction[1] = instruction[1].Replace("0x", "");
                            uint address = Convert.ToUInt32(instruction[1], 16);
                            address >>= 2;
                            instructionBytes[0] = (byte)(instructionBytes[0] | ((address >> 24) & 0x03));
                            instructionBytes[1] = (byte)((address>>16) & 0xFF);
                            instructionBytes[2] = (byte)((address>>8) & 0xFF);
                            instructionBytes[3] = (byte)(address & 0xFF);

                            break;
                        }
                    case "nop":
                        {
                            break;
                        }
                    default:
                        {
                            Log.Error($"CustomHook: Unknown instruction {instruction[0]}");
                            break;
                        }
                }
                bytes.AddRange(instructionBytes);
            }

            return bytes;
        }
        public void InsertHook(ulong targetAddress, int targetInstructionSize, ulong freeAddress)
        {
            _targetAddress = targetAddress;
            _targetInstructionSize = targetInstructionSize;
            _freeAddress = freeAddress;

            List<byte> jmpBack = ConvertAsm([$"jmp 0x{(_targetAddress + (ulong)_targetInstructionSize):X}"]); //($"JMP 0x{(_targetAddress + (ulong)_targetInstructionSize):X}");
            _hookSize = (ulong)(_targetInstructionSize + _bytes.Count + jmpBack.Count);

            byte[] freeBytes = Memory.ReadByteArray(_freeAddress, (int)_hookSize);

            if (freeBytes.Any(b => b != 0x00))
            {
                Log.Error($"CustomHook: Free space at 0x{_freeAddress:X} is not empty!");
                return;
            }

            byte[] first = Memory.ReadByteArray(_targetAddress, _targetInstructionSize);

            Memory.WriteByteArray(_freeAddress, first);
            Memory.WriteByteArray(_freeAddress + (ulong)_targetInstructionSize, _bytes.ToArray());
            Memory.WriteByteArray(_freeAddress + (ulong)_targetInstructionSize + (ulong) _bytes.Count, jmpBack.ToArray());

        }

        public void RemoveHook()
        {
            Memory.WriteByteArray(_targetAddress, Memory.ReadByteArray(_freeAddress, (int)_targetInstructionSize));
            Memory.WriteByteArray(_freeAddress, new byte[_hookSize]);

            _hookSize = 0;
            _targetAddress = 0;
            _targetInstructionSize = 0;
            _freeAddress = 0;
        }
    }
}
