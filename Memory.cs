namespace AsembSimLib
{
    public class Memory
    {
        public const int MEMORY_BLOCKS_LEN = 16; // 4 memory blocks equate to 1 kilobyte
        public const int TOTAL_BYTE_COUNT = MEMORY_BLOCKS_LEN * MemoryBlock.MEMORY_SIZE;

        private readonly MemoryBlock[] memoryBlocks = new MemoryBlock[MEMORY_BLOCKS_LEN];

        public Memory()
        {
            for (int i = 0; i < MEMORY_BLOCKS_LEN; i++)
                memoryBlocks[i] = new MemoryBlock();
        }

        public byte GetAtIndex(byte blockIdx, byte memIdx)
        {
            if (blockIdx >= MEMORY_BLOCKS_LEN) return 0;
            return memoryBlocks[blockIdx].GetAtIndex(memIdx);
        }

        public void SetAtIndex(byte blockIdx, byte memIdx, byte data)
        {
            if (blockIdx >= MEMORY_BLOCKS_LEN) return;
            memoryBlocks[blockIdx].SetAtIndex(memIdx, data);
        }

        public byte[] GetFullDataCopy()
        {
            byte[] dataCopy = new byte[MEMORY_BLOCKS_LEN * MemoryBlock.MEMORY_SIZE];
            byte blockIdx = 0;
            for (int i = 0; i < dataCopy.Length; i++)
            {
                dataCopy[i] =  memoryBlocks[blockIdx].GetAtIndex((byte)(i % MemoryBlock.MEMORY_SIZE));
                if (i != 0 && i % MemoryBlock.MEMORY_SIZE == 0)
                    blockIdx++;
            }
            return dataCopy;
        }

        public MemoryBlock GetMemoryBlock(byte blockIdx)
        {
            if (blockIdx >= MEMORY_BLOCKS_LEN) return new MemoryBlock();

            return memoryBlocks[blockIdx];
        }
    }
}
