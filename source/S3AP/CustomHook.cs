using Archipelago.Core.Util;
using Avalonia;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography;
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

        public ulong _freeAddress;

        private ulong _hookSize;
        public CustomHook(List<string> asm) { 
            _asm = asm;
            _bytes = ConvertAsm(asm);
            //_bytes.Reverse();
            _targetAddress = 0;
            _targetInstructionSize = 0;
            _freeAddress = 0;
        }

        public void ReplaceAsm(List<string> asm)
        {
            if (_targetAddress == 0 && _freeAddress == 0)
            {
                Log.Warning("can't run ReplaceAsm on uninserted hook");
                return;
            }
            ulong targetAddress = _targetAddress;
            int targetInstructionSize = _targetInstructionSize;
            ulong freeAddress = _freeAddress;
            
            RemoveHook();
            _asm = asm;
            _bytes = ConvertAsm(asm);
            InsertHook(targetAddress, targetInstructionSize, freeAddress);
        }

        public static List<byte> ConvertAsm(List<string> asm)
        {
            List<byte> bytes = new List<byte>();
            for (int i = 0; i < asm.Count; i++)
            {
                byte[] instructionBytes = new byte[4];
                asm[i] = asm[i].Replace(", ", " ");
                string[] instruction = asm[i].Split([' ', ',']);
                string[] tempsplit;
                uint address;
                ushort upper;
                uint opcode = 0;
                uint rs = 0;
                uint rt = 0;
                uint rd = 0;
                uint shamt = 0;
                uint funct = 0;
                uint immed = 0;
                //uint encoding = 0;
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
                            address = Convert.ToUInt32(instruction[1], 16);
                            address >>= 2;
                            instructionBytes[0] = (byte)(instructionBytes[0] | ((address >> 24) & 0x03));
                            instructionBytes[1] = (byte)((address>>16) & 0xFF);
                            instructionBytes[2] = (byte)((address>>8) & 0xFF);
                            instructionBytes[3] = (byte)(address & 0xFF);
                            instructionBytes.Reverse();
                            bytes.AddRange(instructionBytes);
                            break;
                        }
                    case "nop":
                        {
                            bytes.AddRange(instructionBytes);
                            break;
                        }
                    case "la":
                        {
                            if (instruction.Length != 3)
                            {
                                Log.Error($"CustomHook: Invalid {instruction[0]} instruction format at line {i + 1}, length was {instruction.Length}");
                                break;
                            }
                            if (!instruction[1].StartsWith("$t"))
                            {
                                Log.Error($"CustomHook: Invalid {instruction[0]} instruction register at line {i + 1} (only $t0 - $t7 are supported)");
                                break;
                            }
                            opcode = 0xF; //lui

                            byte regNum = Convert.ToByte(instruction[1].Replace("$t", ""));
                            rt = (uint) 0x8 + regNum;
                            rs = 0;

                            instruction[2] = instruction[2].Replace("0x", "");
                            address = Convert.ToUInt32(instruction[2], 16);
                            upper = (ushort)(address >> 16);
                            upper++;

                            immed = upper;

                            bytes.AddRange(ConvertToBytes(opcode, rs, rt, immed));

                            opcode = 0x9; //addiu
                            immed = address & 0xFFFF;
                            rs = rt;

                            bytes.AddRange(ConvertToBytes(opcode, rs, rt, immed));

                            break;
                            //instructionBytes[0] = 0x3C;
                            
                            
                            //instructionBytes[1] = (byte)(8 + regNum); // $t0 - $t7
                            
                            
                            //instructionBytes[2] = (byte)((upper >> 8) & 0xFF);
                            //instructionBytes[3] = (byte)((upper) & 0xFF);
                            //Log.Information($"instruction: {BitConverter.ToString(instructionBytes)}");
                            //bytes.AddRange(instructionBytes);
                            //instructionBytes = new byte[4];
                            //instructionBytes[0] = 0x25;
                            //instructionBytes[1] = (byte)((regNum << 5) | (regNum) | 8);
                            //instructionBytes[2] = (byte)((address >> 8) & 0xFF);
                            //instructionBytes[3] = (byte)(address & 0xFF);
                            //Log.Information($"instruction: {BitConverter.ToString(instructionBytes)}");
                            //break;
                        }
                    case "lw":
                        if (instruction.Length != 3)
                        {
                            Log.Error($"CustomHook: Invalid {instruction[0]} instruction format at line {i + 1}, length was {instruction.Length}");
                            break;
                        }
                        opcode = 0x23; //lw

                        //0($t0)
                        tempsplit = instruction[2].Replace("$t", "").Replace(")", "").Split('(');

                        if (tempsplit.Length != 2)
                        {
                            Log.Error($"CustomHook: tempsplit didn't work as intended (length = {tempsplit.Length})");
                            break;
                        }

                        rs = (uint)0x8 + Convert.ToByte(tempsplit[1]);
                        rt = (uint)0x8 + Convert.ToByte(instruction[1].Replace("$t", ""));

                        immed = Convert.ToUInt32(tempsplit[0].Replace("0x", ""), 16);

                        bytes.AddRange(ConvertToBytes(opcode, rs, rt, immed));
                        break;

                    case "sw":
                        if (instruction.Length != 3)
                        {
                            Log.Error($"CustomHook: Invalid {instruction[0]} instruction format at line {i + 1}, length was {instruction.Length}");
                            break;
                        }
                        opcode = 0x2B; //sw

                        //0($t0)
                        tempsplit = instruction[2].Replace("$t", "").Replace(")", "").Split('(');

                        if (tempsplit.Length != 2)
                        {
                            Log.Error($"CustomHook: tempsplit didn't work as intended (length = {tempsplit.Length})");
                            break;
                        }

                        rs = (uint)0x8 + Convert.ToByte(tempsplit[1]);
                        rt = (uint)0x8 + Convert.ToByte(instruction[1].Replace("$t", ""));

                        immed = Convert.ToUInt32(tempsplit[0].Replace("0x", ""), 16);

                        bytes.AddRange(ConvertToBytes(opcode, rs, rt, immed));
                        break;

                    case "ori":
                        if (instruction.Length != 4)
                        {
                            Log.Error($"CustomHook: Invalid {instruction[0]} instruction format at line {i + 1}, length was {instruction.Length}");
                            break;
                        }
                        opcode = 0x0D; //ori


                        rs = (uint)0x8 + Convert.ToByte(instruction[2].Replace("$t", ""));
                        rt = (uint)0x8 + Convert.ToByte(instruction[1].Replace("$t", ""));

                        immed = Convert.ToUInt32(instruction[3].Replace("0x", ""), 16) & 0xFFFF;

                        bytes.AddRange(ConvertToBytes(opcode, rs, rt, immed));
                        break;
                    case "or":
                        if (instruction.Length != 4)
                        {
                            Log.Error($"CustomHook: Invalid {instruction[0]} instruction format at line {i + 1}, length was {instruction.Length}");
                            break;
                        }
                        opcode = 0; //r type
                        funct = 0x25; //or
                        rd = (uint)0x8 + Convert.ToByte(instruction[1].Replace("$t", ""));
                        rs = (uint)0x8 + Convert.ToByte(instruction[2].Replace("$t", ""));
                        rt = (uint)0x8 + Convert.ToByte(instruction[3].Replace("$t", ""));
                        bytes.AddRange(ConvertToBytes(rs, rt, rd, shamt, funct));
                        break;
                    default:
                        {
                            Log.Error($"CustomHook: Unknown instruction {instruction[0]}");
                            break;
                        }
                }
                //bytes.AddRange(instructionBytes);
            }

            return bytes;
        }
        public void LogHookBytes()
        {
            if (_bytes.Count%4 != 0)
            {
                Log.Error("Not mult of 4 somehow like why");
            }
            for (int i = 0; i < _bytes.Count; i+=4)
            {
                
                Log.Information($"line {i}: {Convert.ToHexString([_bytes[i], _bytes[i+1], _bytes[i+2], _bytes[i+3]])}");
            }
        }
        private static byte[] ConvertToBytes(uint opcode, uint rs, uint rt, uint immed)
        {
            uint encoding = 0;
            encoding |= opcode << 26;
            encoding |= rs << 21;
            encoding |= rt << 16;
            encoding |= immed;
            byte[] bytes = BitConverter.GetBytes(encoding);
            //bytes.Reverse();
            return bytes;
        }
        private static byte[] ConvertToBytes(uint rs, uint rt, uint rd, uint shamt, uint funct)
        {
            uint encoding = 0;
            encoding |= rs << 21;
            encoding |= rt << 16;
            encoding |= rd << 11;
            encoding |= shamt << 6;
            encoding |= funct;
            byte[] bytes = BitConverter.GetBytes(encoding);
            //bytes.Reverse();
            return bytes;
        }
        public void InsertHook(ulong targetAddress, int targetInstructionSize, ulong freeAddress)
        {
            if (_targetAddress != 0 && _freeAddress != 0)
            {
                Log.Warning("can't run InsertHook on already inserted hook");
                return;
            }
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
            List<byte> jmpto = ConvertAsm([$"jmp 0x{(_freeAddress):X}"]);
            Memory.WriteByteArray(_targetAddress, jmpto.ToArray());
            Log.Information($"jmpto: {Convert.ToHexString([jmpto[0], jmpto[1], jmpto[2], jmpto[3]])}");

            Memory.WriteByteArray(_freeAddress, first);
            Log.Information($"first: {Convert.ToHexString([first[0], first[1], first[2], first[3]])}");

            Memory.WriteByteArray(_freeAddress + (ulong)_targetInstructionSize, _bytes.ToArray());
            Memory.WriteByteArray(_freeAddress + (ulong)_targetInstructionSize + (ulong) _bytes.Count, jmpBack.ToArray());

            Log.Information("Hook is in");
            
        }

        public void RemoveHook()
        {
            if (_targetAddress == 0 && _freeAddress == 0)
            {
                Log.Warning("can't run RemoveHook on uninserted hook");
                return;
            }
            Memory.WriteByteArray(_targetAddress, Memory.ReadByteArray(_freeAddress, (int)_targetInstructionSize));
            Memory.WriteByteArray(_freeAddress, new byte[_hookSize]);

            _hookSize = 0;
            _targetAddress = 0;
            _targetInstructionSize = 0;
            _freeAddress = 0;
        }
    }
}
