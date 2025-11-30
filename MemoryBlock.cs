namespace AsembSimLib
{
    public class MemoryBlock
    {
        public const int MEMORY_SIZE = byte.MaxValue + 1;

        private readonly byte[] data = new byte[MEMORY_SIZE];

        public byte GetAtIndex(byte index)
        {
            return data[index];
        }

        public void SetAtIndex(byte index, byte data)
        {
            this.data[index] = data;
        }

        public byte[] GetDataCopy()
        {
            byte[] dataCopy = new byte[MEMORY_SIZE];
            for (int i = 0; i < MEMORY_SIZE; i++)
                dataCopy[i] = data[i];
            return dataCopy;
        }
    }
}
