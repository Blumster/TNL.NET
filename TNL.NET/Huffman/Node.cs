using System;

namespace TNL.NET.Huffman
{
    public class Node
    {
        public Byte Symbol { get; set; }
        public UInt32 Frequency { get; set; }
        public Node Right { get; set; }
        public Node Left { get; set; }

        public UInt32 NumBits { get; set; }
        public UInt32 Code { get; set; }

        public Boolean IsLeaf()
        {
            return Left == null && Right == null;
        }
    }
}
