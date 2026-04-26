using System.Runtime.InteropServices;

namespace Poker.Game
{
    public class LutGenerator
    {
        public void GenerateAndSaveFlushLUT()
        {
            Console.WriteLine("Generowanie LUT dla rąk typu Flush (Maski bitowe)...");

            int tableSize = 8192;
            ushort[] flushLut = new ushort[tableSize];
            int validCombinationsCount = 0;

            for (int mask = 0; mask < tableSize; mask++)
            {
                int bitCount = 0;
                for (int i = 0; i < 13; i++)
                {
                    if ((mask & (1 << i)) != 0) bitCount++;
                }

                if (bitCount >= 5 && bitCount <= 7)
                {
                    var flushHand = new List<Card>(bitCount);

                    for (int i = 0; i < 13; i++)
                    {
                        if ((mask & (1 << i)) != 0)
                        {
                            flushHand.Add(new Card(i + 2, 's'));
                        }
                    }

                    int score = Evaluate(flushHand);
                    flushLut[mask] = (ushort)score;

                    validCombinationsCount++;
                }
            }

            File.WriteAllBytes("flush_lut.dat", MemoryMarshal.AsBytes(flushLut.AsSpan()).ToArray());
            Console.WriteLine($"Zapisano tablicę wielkości: {tableSize * 2} bajtów. Liczba unikalnych układów Flush: {validCombinationsCount}");
        }

        public void GenerateAndSaveLUTNonFlush()
        {
            Console.WriteLine("Generowanie LUT (Combinadics)...");

            int totalCombinations = 50388;
            ushort[] lut = new ushort[totalCombinations];

            int[] currentCombo = new int[7];
            int[] rankCounts = new int[13];

            void Generate(int cardIndex, int startRank)
            {
                if (cardIndex == 7)
                {
                    int hashIndex = GetIndexNonFlush(currentCombo);

                    var hand7 = new List<Card>(7);
                    for (int i = 0; i < 7; i++)
                    {
                        char suit = (i % 4) switch { 0 => 'c', 1 => 'd', 2 => 'h', _ => 's' };
                        hand7.Add(new Card(currentCombo[i] + 2, suit));
                    }

                    int score = Evaluate(hand7);
                    lut[hashIndex] = (ushort)score;

                    return;
                }

                for (int r = startRank; r < 13; r++)
                {
                    if (rankCounts[r] < 4)
                    {
                        rankCounts[r]++;
                        currentCombo[cardIndex] = r;
                        Generate(cardIndex + 1, r);
                        rankCounts[r]--;
                    }
                }
            }

            Generate(0, 0);

            File.WriteAllBytes("nonflush_lut.dat", MemoryMarshal.AsBytes(lut.AsSpan()).ToArray());
            Console.WriteLine("Zapisano tablicę wielkości: " + (totalCombinations * 2) + " bajtów.");
        }

        private static readonly int[,] NChooseK = BuildBinomialTable();
        private static int[,] BuildBinomialTable()
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
        private static int GetIndexNonFlush(int[] ranks)
        {
            int index = 0;
            for (int i = 0; i < 7; i++)
            {
                int k = ranks[i] + i;
                index += NChooseK[k, i + 1];
            }
            return index;
        }
        
        public void GenerateAndSaveLUT7()
        {
            Console.WriteLine("Generating LUT, this may take a while...");

            int totalCombinations = 133784560;
            ushort[] lut = new ushort[totalCombinations];

            int[,] combo21 =
            {
                {1,1,1,1,1,0,0}, {1,1,1,1,0,1,0}, {1,1,1,1,0,0,1}, {1,1,1,0,1,1,0}, {1,1,1,0,1,0,1},
                {1,1,1,0,0,1,1}, {1,1,0,1,1,1,0}, {1,1,0,1,1,0,1}, {1,1,0,1,0,1,1}, {1,1,0,0,1,1,1},
                {1,0,1,1,1,1,0}, {1,0,1,1,1,0,1}, {1,0,1,1,0,1,1}, {1,0,1,0,1,1,1}, {1,0,0,1,1,1,1},
                {0,1,1,1,1,1,0}, {0,1,1,1,1,0,1}, {0,1,1,1,0,1,1}, {0,1,1,0,1,1,1}, {0,1,0,1,1,1,1},
                {0,0,1,1,1,1,1}
            };

            Parallel.For(6, 52, c0 =>
            {
                for (int c1 = 5; c1 < c0; c1++)
                    for (int c2 = 4; c2 < c1; c2++)
                        for (int c3 = 3; c3 < c2; c3++)
                            for (int c4 = 2; c4 < c3; c4++)
                                for (int c5 = 1; c5 < c4; c5++)
                                    for (int c6 = 0; c6 < c5; c6++)
                                    {
                                        int[] cards = { c0, c1, c2, c3, c4, c5, c6 };

                                        int hash = GetBinomial(c0, 7) + GetBinomial(c1, 6) + GetBinomial(c2, 5) +
                                                   GetBinomial(c3, 4) + GetBinomial(c4, 3) + GetBinomial(c5, 2) +
                                                   GetBinomial(c6, 1);

                                        int maxScore = 0;

                                        for (int i = 0; i < 21; i++)
                                        {
                                            var hand5 = new List<Card>(5);
                                            for (int j = 0; j < 7; j++)
                                            {
                                                if (combo21[i, j] == 1)
                                                {
                                                    int val = (cards[j] / 4) + 2;
                                                    char suit = (cards[j] % 4) switch { 0 => 'd', 1 => 'h', 2 => 's', _ => 'c' };
                                                    hand5.Add(new Card(val, suit));
                                                }
                                            }

                                            int score = Evaluate(hand5);
                                            if (score > maxScore)
                                            {
                                                maxScore = score;
                                            }
                                        }

                                        lut[hash] = (ushort)maxScore;
                                    }
            });

            Console.WriteLine("Saving to file...");

            byte[] byteData = MemoryMarshal.AsBytes(lut.AsSpan()).ToArray();
            File.WriteAllBytes("lut.dat", byteData);

            Console.WriteLine("Saved to lut.dat");
        }

        private int GetCombinationIndex7(List<Card> combination)
        {
            var cardInts = combination.Select(c =>
            {
                int suitIndex = c.Suit switch { 'd' => 0, 'h' => 1, 's' => 2, 'c' => 3, _ => 0 };
                return (c.Value - 2) * 4 + suitIndex;
            })
            .OrderByDescending(x => x)
            .ToList();

            int index = 0;
            int k = cardInts.Count;

            for (int i = 0; i < cardInts.Count; i++)
            {
                index += GetBinomial(cardInts[i], k);
                k--;
            }

            return index;
        }

        private int GetBinomial(int n, int k)
        {
            if (k < 0 || k > n) return 0;
            if (k == 0 || k == n) return 1;

            if (k == 1) return n;
            if (k == 2) return n * (n - 1) / 2;
            if (k == 3) return n * (n - 1) * (n - 2) / 6;
            if (k == 4) return n * (n - 1) * (n - 2) * (n - 3) / 24;
            if (k == 5) return n * (n - 1) * (n - 2) * (n - 3) * (n - 4) / 120;

            if (k == 6) return (int)((long)n * (n - 1) * (n - 2) * (n - 3) * (n - 4) * (n - 5) / 720);
            if (k == 7) return (int)((long)n * (n - 1) * (n - 2) * (n - 3) * (n - 4) * (n - 5) * (n - 6) / 5040);

            int result = 1;
            for (int i = 1; i <= k; i++)
            {
                result = result * (n - i + 1) / i;
            }
            return result;
        }

        private int Evaluate(List<Card> hand)
        {
            var byValue = hand.GroupBy(c => c.Value)
                              .OrderByDescending(g => g.Count())
                              .ThenByDescending(g => g.Key)
                              .ToList();

            var distinctValues = byValue.Select(g => g.Key).OrderByDescending(v => v).ToList();
            var flushGroup = hand.GroupBy(c => c.Suit).FirstOrDefault(g => g.Count() >= 5);

            int x;
            if ((x = IsStraightFlush(flushGroup)) != 0) return x;
            if ((x = IsFourOfAKind(byValue)) != 0) return x;
            if ((x = IsFullHouse(byValue)) != 0) return x;
            if ((x = IsFlush(flushGroup)) != 0) return x;
            if ((x = IsStraight(distinctValues)) != 0) return x;
            if ((x = IsThreeOfAKind(byValue)) != 0) return x;
            if ((x = IsTwoPair(byValue)) != 0) return x;
            if ((x = IsPair(byValue)) != 0) return x;

            return IsHighCard(distinctValues);
        }

        private int IsStraightFlush(IEnumerable<Card>? flushGroup)
        {
            if (flushGroup == null) return 0;
            var values = flushGroup.Select(c => c.Value).Distinct().OrderByDescending(v => v).ToList();

            for (int i = 0; i <= values.Count - 5; i++)
            {
                if (values[i] - values[i + 4] == 4) return 7453 + (values[i] - 5);
            }

            if (values.Contains(14) && values.Contains(5) && values.Contains(4) &&
                values.Contains(3) && values.Contains(2)) return 7453;

            return 0;
        }

        private int IsFourOfAKind(List<IGrouping<int, Card>> byValue)
        {
            if (byValue[0].Count() < 4 || byValue.Count < 2) return 0;

            int fourValue = byValue[0].Key;
            int kicker = byValue[1].Key;

            int kickerRank = kicker > fourValue ? kicker - 3 : kicker - 2;
            return 7297 + (12 * (fourValue - 2)) + kickerRank;
        }

        private int IsFullHouse(List<IGrouping<int, Card>> byValue)
        {
            if (byValue[0].Count() < 3 || byValue.Count < 2 || byValue[1].Count() < 2) return 0;

            int threeValue = byValue[0].Key;
            int pairValue = byValue[1].Key;

            int pairRank = pairValue > threeValue ? pairValue - 3 : pairValue - 2;
            return 7141 + (12 * (threeValue - 2)) + pairRank;
        }

        private int IsFlush(IEnumerable<Card>? flushGroup)
        {
            if (flushGroup == null) return 0;

            var top5 = flushGroup.Select(c => c.Value)
                                 .OrderByDescending(v => v)
                                 .Take(5)
                                 .Select(v => v - 2)
                                 .ToList();

            int index = GetBinomial(top5[0], 5) + GetBinomial(top5[1], 4) +
                        GetBinomial(top5[2], 3) + GetBinomial(top5[3], 2) +
                        GetBinomial(top5[4], 1);

            int[] sfIndices = { 0, 5, 20, 55, 125, 251, 461, 791, 792, 1286 };
            int sfCount = sfIndices.Count(x => x <= index);

            return 5864 + (index - sfCount);
        }

        private int IsStraight(List<int> distinctValues)
        {
            if (distinctValues.Count < 5) return 0;

            for (int i = 0; i <= distinctValues.Count - 5; i++)
            {
                if (distinctValues[i] - distinctValues[i + 4] == 4) return 5854 + (distinctValues[i] - 5);
            }

            if (distinctValues.Contains(14) && distinctValues.Contains(5) &&
                distinctValues.Contains(4) && distinctValues.Contains(3) &&
                distinctValues.Contains(2)) return 5854;

            return 0;
        }

        private int IsThreeOfAKind(List<IGrouping<int, Card>> byValue)
        {
            if (byValue[0].Count() < 3 || byValue.Count < 3) return 0;

            int threeValue = byValue[0].Key;
            int k1 = byValue[1].Key;
            int k2 = byValue[2].Key;

            k1 = k1 > threeValue ? k1 - 3 : k1 - 2;
            k2 = k2 > threeValue ? k2 - 3 : k2 - 2;

            int kickerIndex = (k1 * (k1 - 1) / 2) + k2;
            return 4996 + ((threeValue - 2) * 66) + kickerIndex;
        }

        private int IsTwoPair(List<IGrouping<int, Card>> byValue)
        {
            if (byValue[0].Count() < 2 || byValue.Count < 3 || byValue[1].Count() < 2) return 0;

            int highPair = byValue[0].Key;
            int lowPair = byValue[1].Key;
            int kicker = byValue.Select(g => g.Key).Where(k => k != highPair && k != lowPair).Max();

            int p1 = highPair - 2;
            int p2 = lowPair - 2;
            int pairIndex = (p1 * (p1 - 1) / 2) + p2;

            int kickerRank = kicker - 2;
            if (kicker > highPair) kickerRank -= 2;
            else if (kicker > lowPair) kickerRank -= 1;

            return 4138 + (pairIndex * 11) + kickerRank;
        }

        private int IsPair(List<IGrouping<int, Card>> byValue)
        {
            if (byValue[0].Count() < 2 || byValue.Count < 4) return 0;

            int pairValue = byValue[0].Key;
            int k1 = byValue[1].Key;
            int k2 = byValue[2].Key;
            int k3 = byValue[3].Key;

            k1 = k1 > pairValue ? k1 - 3 : k1 - 2;
            k2 = k2 > pairValue ? k2 - 3 : k2 - 2;
            k3 = k3 > pairValue ? k3 - 3 : k3 - 2;

            int kickerIndex = GetBinomial(k1, 3) + GetBinomial(k2, 2) + GetBinomial(k3, 1);
            return 1278 + ((pairValue - 2) * 220) + kickerIndex;
        }

        private int IsHighCard(List<int> distinctValues)
        {
            if (distinctValues.Count < 5) return 0;

            var top5 = distinctValues.Take(5).Select(v => v - 2).ToList();

            int index = GetBinomial(top5[0], 5) + GetBinomial(top5[1], 4) +
                        GetBinomial(top5[2], 3) + GetBinomial(top5[3], 2) +
                        GetBinomial(top5[4], 1);

            int[] sfIndices = { 0, 5, 20, 55, 125, 251, 461, 791, 792, 1286 };
            int sfCount = sfIndices.Count(x => x <= index);

            return 1 + (index - sfCount);
        }

    }
}
