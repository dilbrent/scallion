using System;
using System.Collections.Generic;
using System.Linq;

namespace scallion
{
	public class BitmaskPatternsTuple
	{
        public BitmaskPatternsTuple() { }
        public BitmaskPatternsTuple(uint[] bitmask, IEnumerable<uint[]> patterns)
        {
            Bitmask = bitmask;
            patterns = Patterns.ToList();
        }
		public uint[] Bitmask;
		public IList<uint[]> Patterns;
	}

    public abstract class ToolConfig
    {
        protected RegexPattern _regex;
        protected IList<BitmaskPatternsTuple> _bitmaskPatterns;
        public ToolConfig(string pattern)
        {
            _regex = CreateRegexPattern(pattern);
            _bitmaskPatterns = GenerateBitmaskPatterns();
        }
        public bool SinglePattern
        {
            get { return BitmaskPatterns.SelectMany(i => i.Patterns).Count() == 1; }
        }
        public IList<BitmaskPatternsTuple> BitmaskPatterns 
        {
            get { return _bitmaskPatterns; } 
        }
        public int NumberOfWords
        {
            get { return BitmaskPatterns[0].Bitmask.Length; }
        }
        public void CreateHashTableAndPackPatterns(out ushort[] hashTable, out uint[] packedPatterns, out int maxKeyCollisions)
        {
            //Dictionary< FNV10 hash of a pattern/patterns , list of patterns >
            Dictionary<ushort, List<uint[]>> patterns = BitmaskPatterns
                .SelectMany(i => i.Patterns)
                .Select(i => new KeyValuePair<ushort, uint[]>(Util.FNV10(i), i))
                .GroupBy(i => i.Key)
                .ToDictionary(i => i.Key,  i => i.Select(j => j.Value).ToList());

            int packedPatternsLength = BitmaskPatterns
                .SelectMany(i => i.Patterns)
                .SelectMany(i => i).Count();

            hashTable = new ushort[1024];
            packedPatterns = new uint[packedPatternsLength];
            maxKeyCollisions = 0;
            ushort currentPackedIndex = 0;
            //iterate over all fnv10keys.. add them to hash table and pack their patterns
            foreach (ushort fnv10Key in patterns.Keys.OrderBy(i => i))
            {
                //set index in hash table
                hashTable[fnv10Key] = currentPackedIndex;
                //update the max number of key collisions
                if (patterns[fnv10Key].Count > maxKeyCollisions)
                    maxKeyCollisions = patterns[fnv10Key].Count;
                //copy patterns to packed patterns
                foreach (uint[] pattern in patterns[fnv10Key])
                {
                    Array.Copy(pattern, 0, packedPatterns, currentPackedIndex, pattern.Length);
                    currentPackedIndex += (ushort)pattern.Length;
                }
            }
        }
		public int NumberOfMasks
		{
            get { return BitmaskPatterns.Count; }
		}
        protected abstract RegexPattern CreateRegexPattern(string pattern);
        public abstract TimeSpan PredictRuntime(int hashRate);
        public abstract bool CheckMatch(RSAWrapper rsa);
        protected abstract IList<BitmaskPatternsTuple> GenerateBitmaskPatterns();

    }
}

