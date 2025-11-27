namespace AsembSimLib
{
    public class CPU
    {
        public static readonly int MAX_CODE_SIZE = (Memory.MEMORY_BLOCKS_LEN - 2) * MemoryBlock.MEMORY_SIZE;
        public static readonly byte ROUT = 0x0F;

        byte[] registers = new byte[16];
        byte counterReg = 0;
        byte counter2Reg = 0;
        byte cmpflagsReg = 0;
        readonly Memory memory = new();

        public byte ProgramCounter { get { return counterReg; } }
        public byte ProgramBlockCounter { get { return counter2Reg; } }
        public byte[] FullMemoryData { get { return memory.GetFullDataCopy(); } }

        public void LoadCodeIntoMemory(byte[] code)
        {
            if (code.Length != MAX_CODE_SIZE) return;

            List<byte[]> chunks = new();

            for (int i = 0; i < code.Length; i += MemoryBlock.MEMORY_SIZE)
            {
                int size = Math.Min(MemoryBlock.MEMORY_SIZE, code.Length - i);
                byte[] chunk = new byte[size];
                Buffer.BlockCopy(code, i, chunk, 0, size);
                chunks.Add(chunk);
            }

            for (int i = 0; i < chunks.Count; i++)
            {
                MemoryBlock mb = memory.GetMemoryBlock((byte)i);
                if (mb == null) return;
                for (int j = 0; j < chunks[i].Length; j++)
                    mb.SetAtIndex((byte)j, chunks[i][j]);
            }
        }

        public byte[] GetMemoryBlockData(byte blockIdx)
        {
            MemoryBlock mb = memory.GetMemoryBlock(blockIdx);
            if (mb == null) return new byte[MemoryBlock.MEMORY_SIZE];
            return mb.GetDataCopy();
        }

        public (bool, byte?) Clock(byte[] instructionReg, byte? @in = null)
        {
            return Clock(instructionReg, out _, @in);
        }

        // You want unreadable code? I shall give you unreadable code!
        // Edit 1 year later: I am having trouble reading this ._.
        public (bool, byte?) Clock(byte[] instructionReg, out bool isReadingKey, byte? @in = null)
        {
            isReadingKey = false;
            try
            {
                byte p0 = instructionReg[0];
                byte p1 = instructionReg[1];
                byte p2 = instructionReg[2];
                byte p3 = instructionReg[3];

                byte p1Reg = (instructionReg[1]) < registers.Length ? registers[instructionReg[1]] : (byte)0x00;
                byte p2Reg = (instructionReg[2]) < registers.Length ? registers[instructionReg[2]] : (byte)0x00;
                byte p3Reg = (instructionReg[3]) < registers.Length ? registers[instructionReg[3]] : (byte)0x00;

                (bool, byte?) result = (true, null);

                if (p0 < 0x20)
                {
                    switch (p0)
                    {
                        case 0x01:
                            if (instructionReg[3] < 0 || instructionReg[3] >= registers.Length) break;
                            registers[instructionReg[3]] = (byte)(p1Reg + p2Reg);
                            if (instructionReg[3] == ROUT) result.Item2 = registers[instructionReg[3]];
                            break;
                        case 0x02:
                            if (instructionReg[3] < 0 || instructionReg[3] >= registers.Length) break;
                            registers[instructionReg[3]] = (byte)(p1Reg - p2Reg);
                            if (instructionReg[3] == ROUT) result.Item2 = registers[instructionReg[3]];
                            break;
                        case 0x03:
                            if (instructionReg[3] < 0 || instructionReg[3] >= registers.Length) break;
                            registers[instructionReg[3]] = (byte)(p1Reg * p2Reg);
                            if (instructionReg[3] == ROUT) result.Item2 = registers[instructionReg[3]];
                            break;
                        case 0x04:
                            if (instructionReg[3] < 0 || instructionReg[3] >= registers.Length) break;
                            if (p2Reg == 0) 
                            {
                                counterReg = 0;
                                counter2Reg = 0;
                                registers = new byte[registers.Length];
                                return (false, null);
                            }
                            registers[instructionReg[3]] = (byte)(p1Reg / p2Reg);
                            if (instructionReg[3] == ROUT) result.Item2 = registers[instructionReg[3]];
                            break;
                        case 0x05:
                            if (instructionReg[3] < 0 || instructionReg[3] >= registers.Length) break;
                            if (p2Reg == 0)
                            {
                                counterReg = 0;
                                counter2Reg = 0;
                                registers = new byte[registers.Length];
                                return (false, null);
                            }
                            registers[instructionReg[3]] = (byte)(p1Reg % p2Reg);
                            if (instructionReg[3] == ROUT) result.Item2 = registers[instructionReg[3]];
                            break;
                        case 0x06:
                            if (instructionReg[3] < 0 || instructionReg[3] >= registers.Length) break;
                            registers[instructionReg[3]] = (byte)(p1Reg & p2Reg);
                            if (instructionReg[3] == ROUT) result.Item2 = registers[instructionReg[3]];
                            break;
                        case 0x07:
                            if (instructionReg[3] < 0 || instructionReg[3] >= registers.Length) break;
                            registers[instructionReg[3]] = (byte)(p1Reg | p2Reg);
                            if (instructionReg[3] == ROUT) result.Item2 = registers[instructionReg[3]];
                            break;
                        case 0x08:
                            if (instructionReg[2] < 0 || instructionReg[2] >= registers.Length) break;
                            registers[instructionReg[2]] = (byte)~p1Reg;
                            if (instructionReg[2] == ROUT) result.Item2 = registers[instructionReg[2]];
                            break;
                        case 0x09:
                            if (instructionReg[3] < 0 || instructionReg[3] >= registers.Length) break;
                            registers[instructionReg[3]] = (byte)(p1Reg ^ p2Reg);
                            if (instructionReg[3] == ROUT) result.Item2 = registers[instructionReg[3]];
                            break;
                        case 0x0A:
                            if (instructionReg[2] < 0 || instructionReg[2] >= registers.Length) break;
                            registers[instructionReg[2]] = p1Reg;
                            if (instructionReg[2] == ROUT) result.Item2 = registers[instructionReg[2]];
                            break;
                        case 0x0B:
                            if (instructionReg[2] < 0 || instructionReg[2] >= registers.Length) break;
                            registers[instructionReg[2]] = p1;
                            if (instructionReg[2] == ROUT) result.Item2 = registers[instructionReg[2]];
                            break;
                        case 0x0C:
                            memory.SetAtIndex(p3Reg, p2Reg, p1Reg);
                            break;
                        case 0x0D:
                            if (instructionReg[3] < 0 || instructionReg[3] >= registers.Length) break;
                            registers[instructionReg[3]] = memory.GetAtIndex(p2Reg, p1Reg);
                            if (instructionReg[3] == ROUT) result.Item2 = registers[instructionReg[3]];
                            break;
                        case 0x0E:
                            memory.SetAtIndex(14, registers[14], p1Reg);
                            registers[14]++;
                            break;
                        case 0x0F:
                            if (instructionReg[1] < 0 || instructionReg[1] >= registers.Length) break;
                            registers[14]--;
                            registers[instructionReg[1]] = memory.GetAtIndex(14, registers[14]);
                            if (instructionReg[1] == ROUT) result.Item2 = registers[instructionReg[1]];
                            break;
                        case 0x10:
                            cmpflagsReg = (byte)(
                                    (p1Reg == p2Reg ? CompareFlags.EQUAL : 0) | 
                                    (p1Reg > p2Reg ? CompareFlags.GREATER : 0) |
                                    (p1Reg < p2Reg ? CompareFlags.LESS : 0)
                                );
                            break;
                        case 0x11:
                            if (instructionReg[1] < 0 || instructionReg[1] >= registers.Length) break;
                            registers[instructionReg[1]]++;
                            if (instructionReg[1] == ROUT) result.Item2 = registers[instructionReg[1]];
                            break;
                        case 0x12:
                            if (instructionReg[1] < 0 || instructionReg[1] >= registers.Length) break;
                            registers[instructionReg[1]]--;
                            if (instructionReg[1] == ROUT) result.Item2 = registers[instructionReg[1]];
                            break;
                    }
                }
                if (p0 >= 0x20)
                {
                    switch (p0)
                    {
                        case 0x20:
                            SetCounter(p1, p2);
                            break;
                        case 0x21:
                            if ((cmpflagsReg & (int)CompareFlags.EQUAL) == (int)CompareFlags.EQUAL) SetCounter(p1, p2);
                            else ProgressCounter();
                            break;
                        case 0x22:
                            if ((cmpflagsReg & (int)CompareFlags.EQUAL) != (int)CompareFlags.EQUAL) SetCounter(p1, p2);
                            else ProgressCounter();
                            break;
                        case 0x23:
                            if ((cmpflagsReg & (int)CompareFlags.LESS) == (int)CompareFlags.LESS) SetCounter(p1, p2);
                            else ProgressCounter();
                            break;
                        case 0x24:
                            if ((cmpflagsReg & (int)CompareFlags.GREATER) == (int)CompareFlags.GREATER) SetCounter(p1, p2);
                            else ProgressCounter();
                            break;
                        case 0x25:
                            if ((cmpflagsReg & (int)CompareFlags.LESS) == (int)CompareFlags.LESS || 
                                (cmpflagsReg & (int)CompareFlags.EQUAL) == (int)CompareFlags.EQUAL) SetCounter(p1, p2);
                            else ProgressCounter();
                            break;
                        case 0x26:
                            if ((cmpflagsReg & (int)CompareFlags.GREATER) == (int)CompareFlags.GREATER ||
                                (cmpflagsReg & (int)CompareFlags.EQUAL) == (int)CompareFlags.EQUAL) SetCounter(p1, p2);
                            else ProgressCounter();
                            break;
                        case 0x27:
                            counterReg = 0;
                            counter2Reg = 0;
                            registers = new byte[registers.Length];
                            return (false, null);
                        case 0x28:
                            if (@in != null)
                            {
                                registers[0] = @in.Value;
                                ProgressCounter();
                            }
                            else isReadingKey = true;
                            break;
                        case 0x29:
                            result = (false, p1Reg);
                            counterReg = 0;
                            counter2Reg = 0;
                            registers = new byte[registers.Length];
                            return result;
                        case 0x2A:
                            memory.SetAtIndex(14, registers[14], counterReg);
                            memory.SetAtIndex(14, (byte)(registers[14] + 1), counter2Reg);
                            counterReg = p1;
                            counter2Reg = p2;
                            registers[14]+=2;
                            break;
                        case 0x2B:
                            registers[14]-=2;
                            counterReg = (byte)(memory.GetAtIndex(14, registers[14]) + 4);
                            counter2Reg = memory.GetAtIndex(14, (byte)(registers[14] + 1));
                            break;
                        default:
                            ProgressCounter();
                            break;
                    }
                }
                else ProgressCounter();

                return result;
            }
            catch (Exception)
            {
                counterReg = 0;
                registers = new byte[registers.Length];
                return (false, null);
            }
        }

        public byte[] GetRegisters()
        {
            return registers;
        }

        private void ProgressCounter()
        {
            byte newVal = (byte)(counterReg + 4);

            if (newVal < counterReg)
            {
                counter2Reg++;
                if (counter2Reg >= Memory.MEMORY_BLOCKS_LEN)
                    counter2Reg = 0;
            }

            counterReg = newVal;
        }

        private void SetCounter(byte p1, byte p2)
        {
            counterReg = p1;
            counter2Reg = p2;
            if (counter2Reg >= Memory.MEMORY_BLOCKS_LEN)
                counter2Reg = 0;
        }
    }
}
