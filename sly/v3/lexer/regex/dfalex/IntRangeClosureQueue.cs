using System.Diagnostics;

namespace sly.v3.lexer.regex.dfalex
{
    /// <summary>
    /// Closure queue containing integers in a limited range.
    /// </summary>
    internal class IntRangeClosureQueue
    {
        readonly int[] bitmask;
        readonly int[] queue;
        int            readpos;
        int            writepos;

        /// <summary>
        /// Create a new IntRangeClosureQueue.
        ///
        /// The queue can contain integer in [0,range)
        /// </summary>
        /// <param name="range"></param>
        public IntRangeClosureQueue(int range)
        {
            bitmask = new int[(range + 31) >> 5];
            queue = new int[bitmask.Length * 32 + 1];
        }

        /// <summary>
        /// Add an integer to the tail of the queue if it's not already present
        /// </summary>
        /// <param name="val">integer to add</param>
        /// <returns>true if the integer was added to the queue, or false f it was not added, because it was already in the queue</returns>
        public bool Add(int val)
        {
            var i = val >> 5;
            var bit = 1 << (val & 31);
            var oldbits = bitmask[i];
            if ((oldbits & bit) == 0)
            {
                bitmask[i] = oldbits | bit;
                queue[writepos] = val;
                if (++writepos >= queue.Length)
                {
                    writepos = 0;
                }

                Debug.Assert(writepos != readpos);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Remove an integer from the head of the queue, if it's non-empty.
        /// </summary>
        /// <returns>the integer removed from the head of the queue, or -1 if the queue was empty.</returns>
        public int Poll()
        {
            if (readpos == writepos)
            {
                return -1;
            }

            var val = queue[readpos];
            if (++readpos >= queue.Length)
            {
                readpos = 0;
            }

            var i = val >> 5;
            var bit = 1 << (val & 31);
            Debug.Assert((bitmask[i] & bit) != 0);
            bitmask[i] &= ~bit;
            Debug.Assert((bitmask[i] & bit) == 0);
            return val;
        }
    }
}