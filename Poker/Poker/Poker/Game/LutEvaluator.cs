namespace Poker.Game
{
    public class LutEvaluator : IHandEvaluator
    {
        private readonly ushort[] _nonFlushLUT;
        private readonly ushort[] _flushLUT;
        private readonly int[,] NChooseK;

        public LutEvaluator(string nonFlushLUTFilePath, string flushLUTFilePath)
        {
            Console.WriteLine("Loading non-flush LUT...");
            byte[] fileBytes = File.ReadAllBytes(nonFlushLUTFilePath);
            _nonFlushLUT = new ushort[fileBytes.Length / 2];
            Buffer.BlockCopy(fileBytes, 0, _nonFlushLUT, 0, fileBytes.Length);
            Console.WriteLine("Non-flush LUT loaded...");

            Console.WriteLine("Loading flush LUT...");
            fileBytes = File.ReadAllBytes(flushLUTFilePath);
            _flushLUT = new ushort[fileBytes.Length / 2];
            Buffer.BlockCopy(fileBytes, 0, _flushLUT, 0, fileBytes.Length);
            Console.WriteLine("Flush LUT loaded...");


            NChooseK = BuildBinomialTable();
        }

        public int Evaluate7(List<Card> hand)
        {
            int flushIndex = GetIndexFlush(hand);
            if (flushIndex != -1)
            {
                return _flushLUT[flushIndex];
            }

            Span<int> ranks = stackalloc int[7];
            for (int i = 0; i < 7; i++)
            {
                ranks[i] = hand[i].Value - 2;
            }

            ranks.Sort();

            return _nonFlushLUT[GetIndexNonFlush(ranks)];
        }

        private int GetIndexNonFlush(ReadOnlySpan<int> ranks)
        {
            int index = 0;
            for (int i = 0; i < 7; i++)
            {
                int k = ranks[i] + i;
                index += NChooseK[k, i + 1];
            }
            return index;
        }

        public int GetIndexFlush(List<Card> cards)
        {
            for (int i = 0; i < 3; i++)
            {
                int suitCount = 1;
                int suitMask = 1 << (cards[i].Value - 2);

                for (int j = i + 1; j < 7; j++)
                {
                    if (cards[i].Suit.Equals(cards[j].Suit))
                    {
                        suitCount++;
                        suitMask |= 1 << (cards[j].Value - 2);
                    }
                }

                if (suitCount >= 5)
                {
                    return suitMask;
                }
            }

            return -1;
        }

        private int[,] BuildBinomialTable()
        {
            int[,] table = new int[19, 8];
            for (int i = 0; i < 19; i++)
            {
                table[i, 0] = 1;
                for (int j = 1; j <= Math.Min(i, 7); j++)
                {
                    if (i == j) table[i, j] = 1;
                    else table[i, j] = table[i - 1, j - 1] + table[i - 1, j];
                }
            }
            return table;
        }
    }
}
