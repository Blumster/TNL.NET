using System;
using System.Security.Cryptography;
using System.Text;

namespace TNL.NET.Utils
{
    using Entities;
    using Huffman;
    using Structs;
    using Types;

    public class BitStream : ByteBuffer
    {
        #region Consts
        public const Single FloatOne = 1.0f;
        public const Single FloatHalf = 0.5f;
        public const Single FloatZero = 0.0f;

        public const Single FloatPi = (Single) Math.PI;
        public const Single Float2Pi = 2.0f * FloatPi;
        public const Single FloatInversePi = 1.0f / FloatPi;
        public const Single FloatHalfPi = 0.5f * FloatPi;
        public const Single Float2InversePi = 2.0f / FloatPi;
        public const Single FloatInverse2Pi = 0.5f / FloatPi;

        public const Single FloatSqrt2 = 1.41421356237309504880f;
        public const Single FloatSqrtHalf = 0.7071067811865475244008443f;

        public static readonly Byte[] BitCounts = { 16, 18, 20, 32 };

        #endregion

        public const UInt32 ResizePad = 1500U;

        protected UInt32 BitNum;
        protected Boolean Error;
        protected Boolean CompressRelative;
        protected Point3F CompressPoint;
        protected UInt32 MaxReadBitNum;

        public UInt32 MaxWriteBitNum;

        protected Byte[] CurrentByte;
        protected ConnectionStringTable StringTable;
        protected readonly Byte[] StringBuffer = new Byte[256];

        public BitStream(Byte[] data, UInt32 bufSize)
            : base(data, bufSize)
        {
            SetMaxSizes(bufSize, bufSize);
            Reset();
            CurrentByte = new Byte[1];
        }

        public BitStream(Byte[] data, UInt32 bufSize, UInt32 maxWriteSize)
            : base(data, bufSize)
        {
            SetMaxSizes(bufSize, maxWriteSize);
            Reset();
            CurrentByte = new Byte[1];
        }

        public BitStream()
        {
            SetMaxSizes(GetBufferSize(), GetBufferSize());
            Reset();
            CurrentByte = new Byte[1];
        }

        protected Boolean ResizeBits(UInt32 newBitsNeeded)
        {
            var newSize = ((MaxWriteBitNum + newBitsNeeded + 7) >> 3) + ResizePad;
            if (!Resize(newSize))
            {
                Error = true;
                return false;
            }

            MaxReadBitNum = newSize << 3;
            MaxWriteBitNum = newSize << 3;
            return true;
        }

        public void SetMaxSizes(UInt32 maxReadSize, UInt32 maxWriteSize)
        {
            MaxReadBitNum = maxReadSize << 3;
            MaxWriteBitNum = maxWriteSize << 3;
        }

        public void SetMaxBitSizes(UInt32 maxReadBitSize, UInt32 maxWriteBitSize)
        {
            MaxReadBitNum = maxReadBitSize;
            MaxWriteBitNum = maxWriteBitSize;
        }

        public void Reset()
        {
            BitNum = 0;
            Error = false;
            CompressRelative = false;
            StringBuffer[0] = 0;
            StringTable = null;
        }

        public void CleanStringBuffer()
        {
            StringBuffer[0] = 0;
        }

        public void SetStringTable(ConnectionStringTable table)
        {
            StringTable = table;
        }

        public void ClearError()
        {
            Error = false;
        }

        public UInt32 GetBytePosition()
        {
            return (BitNum + 7) >> 3;
        }

        public UInt32 GetBitPosition()
        {
            return BitNum;
        }

        public void SetBytePosition(UInt32 newPosition)
        {
            BitNum = newPosition << 3;
        }

        public void SetBitPosition(UInt32 newBitPosition)
        {
            BitNum = newBitPosition;
        }

        public void AdvanceBitPosition(UInt32 numBits)
        {
            SetBitPosition(GetBitPosition() + numBits);
        }

        public UInt32 GetMaxReadBitPosition()
        {
            return MaxReadBitNum;
        }

        public UInt32 GetBitSpaceAvailable()
        {
            return MaxWriteBitNum - BitNum;
        }

        public void ZeroToByteBoundary()
        {
            if ((BitNum & 0x7) != 0)
                WriteInt(0, (Byte) (8 - (BitNum & 0x7)));
        }

        public void WriteInt(UInt32 value, Byte bitCount)
        {
            WriteBits(bitCount, BitConverter.GetBytes(value));
        }

        public UInt32 ReadInt(Byte bitCount)
        {
            var bits = new Byte[4];

            ReadBits(bitCount, bits);

            var ret = BitConverter.ToUInt32(bits, 0);

            if (bitCount == 32)
                return ret;

            ret &= (1U << bitCount) - 1;

            return ret;
        }

        public void WriteIntAt(UInt32 value, Byte bitCount, UInt32 bitPosition)
        {
            var curPos = GetBitPosition();

            SetBitPosition(bitPosition);

            WriteInt(value, bitCount);

            SetBitPosition(curPos);
        }

        public void WriteSignedInt(Int32 value, Byte bitCount)
        {
            if (WriteFlag(value < 0))
                WriteInt((UInt32) (-value), (Byte) (bitCount - 1));
            else
                WriteInt((UInt32) value, (Byte) (bitCount - 1));
        }

        public Int32 ReadSignedInt(Byte bitCount)
        {
            if (ReadFlag())
                return -(Int32) ReadInt((Byte) (bitCount - 1));
            
            return (Int32) ReadInt((Byte) (bitCount - 1));
        }

        public void WriteRangedU32(UInt32 value, UInt32 rangeStart, UInt32 rangeEnd)
        {
            var rangeSize = rangeEnd - rangeStart + 1;
            var rangeBits = Utils.GetNextBinLog2(rangeSize);

            WriteInt(value - rangeStart, (Byte) rangeBits);
        }

        public UInt32 ReadRangedU32(UInt32 rangeStart, UInt32 rangeEnd)
        {
            var rangeSize = rangeEnd - rangeStart + 1;
            var rangeBits = Utils.GetNextBinLog2(rangeSize);

            return ReadInt((Byte) rangeBits) + rangeStart;
        }

        public void WriteEnum(UInt32 enumValue, UInt32 enumRange)
        {
            WriteInt(enumValue, (Byte) Utils.GetNextBinLog2(enumRange));
        }

        public UInt32 ReadEnum(UInt32 enumRange)
        {
            return ReadInt((Byte) Utils.GetNextBinLog2(enumRange));
        }

        public void WriteFloat(Single f, Byte bitCount)
        {
            WriteInt((UInt32) (f * ((1 << bitCount) - 1)), bitCount);
        }

        public Single ReadFloat(Byte bitCount)
        {
            return ReadInt(bitCount) / (Single) ((1 << bitCount) - 1);
        }

        public void WriteSignedFloat(Single f, Byte bitCount)
        {
            WriteSignedInt((Int32) (f * ((1 << (bitCount -1)) -1)), bitCount);
        }

        public Single ReadSignedFloat(Byte bitCount)
        {
            return ReadSignedInt(bitCount) / (Single) ((1 << (bitCount - 1)) - 1);
        }

        public void WriteClassId(UInt32 classId, UInt32 classType, UInt32 classGroup)
        {
            WriteInt(classId, (Byte) NetClassRep.GetNetClassBitSize(classGroup, classType));
        }

        public UInt32 ReadClassId(UInt32 classType, UInt32 classGroup)
        {
            var ret = ReadInt((Byte) NetClassRep.GetNetClassBitSize(classGroup, classType));
            return ret >= NetClassRep.GetNetClassCount(classGroup, classType) ? 0xFFFFFFFFU : ret;
        }

        public void WriteNormalVector(Point3F vec, Byte bitCount)
        {
            var phi = (Single) (Math.Atan2(vec.X, vec.Y) * FloatInversePi);
            var theta = (Single) (Math.Atan2(vec.Z, Math.Sqrt(vec.X * vec.X + vec.Y * vec.Y)) * Float2InversePi);

            WriteSignedFloat(phi, (Byte) (bitCount + 1));
            WriteSignedFloat(theta, bitCount);
        }

        public void ReadNormalVector(ref Point3F vec, Byte bitCount)
        {
            var phi = ReadSignedFloat((Byte)(bitCount + 1)) * FloatPi;
            var theta = ReadSignedFloat(bitCount) * FloatHalfPi;

            vec.X = (Single) (Math.Sin(phi) * Math.Cos(theta));
            vec.Y = (Single) (Math.Cos(phi) * Math.Cos(theta));
            vec.Z = (Single) Math.Sin(theta);
        }

        public static Point3F DumbDownNormal(Point3F vec, Byte bitCount)
        {
            var buffer = new Byte[128];
            var temp = new BitStream(buffer, 128U);

            temp.WriteNormalVector(vec, bitCount);
            temp.SetBitPosition(0U);

            var ret = new Point3F();
            
            temp.ReadNormalVector(ref ret, bitCount);

            return ret;
        }

        public void WriteNormalVector(Point3F vec, Byte angleBitCount, Byte zBitCount)
        {
            if (WriteFlag(Math.Abs(vec.Z) >= (1.0f - (1.0f / zBitCount))))
                WriteFlag(vec.Z < 0.0f);
            else
            {
                WriteSignedFloat(vec.Z, zBitCount);
                WriteSignedFloat((Single) Math.Atan2(vec.X, vec.Y) * FloatInverse2Pi, angleBitCount);
            }
        }

        public void ReadNormalVector(ref Point3F vec, Byte angleBitCount, Byte zBitCount)
        {
            if (ReadFlag())
            {
                vec.Z = ReadFlag() ? -1.0f : 1.0f;
                vec.X = 0.0f;
                vec.Y = 0.0f;
            }
            else
            {
                vec.Z = ReadSignedFloat(zBitCount);

                var angle = Float2Pi * ReadSignedFloat(angleBitCount);

                var mult = (Single) Math.Sqrt(1.0f - vec.Z * vec.Z);
                vec.X = mult * (Single) Math.Cos(angle);
                vec.X = mult * (Single) Math.Sin(angle);
            }
        }

        public void SetPointCompression(Point3F p)
        {
            CompressRelative = true;
            CompressPoint = p;
        }

        public void ClearPointCompression()
        {
            CompressRelative = false;
        }

        public void WritePointCompressed(Point3F p, Single scale)
        {
            var vec = new Point3F();

            var invScale = 1.0f / scale;
            UInt32 type;

            if (CompressRelative)
            {
                vec.X = p.X - CompressPoint.X;
                vec.Y = p.Y - CompressPoint.Y;
                vec.Z = p.Z - CompressPoint.Z;

                var dist = (Single) Math.Sqrt(vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z) * invScale;

                if (dist < (1 << 15))
                    type = 0U;
                else if (dist < (1 << 17))
                    type = 1U;
                else if (dist < (1 << 19))
                    type = 2U;
                else
                    type = 3U;
            }
            else
                type = 3U;

            WriteInt(type, 2);

            if (type != 3U)
            {
                var size = BitCounts[type];

                WriteSignedInt((Int32) (vec.X * invScale), size);
                WriteSignedInt((Int32) (vec.Y * invScale), size);
                WriteSignedInt((Int32) (vec.Z * invScale), size);
            }
            else
            {
                Write(p.X);
                Write(p.Y);
                Write(p.Z);
            }
        }

        public void ReadPointCompressed(ref Point3F p, Single scale)
        {
            var type = ReadInt(2);
            if (type == 3)
            {
                Single x, y, z;

                Read(out x);
                Read(out y);
                Read(out z);

                p.X = x;
                p.Y = y;
                p.Z = z;
            }
            else
            {
                var size = BitCounts[type];

                p.X = ReadSignedInt(size);
                p.Y = ReadSignedInt(size);
                p.Z = ReadSignedInt(size);

                p.X = CompressPoint.X + p.X * scale;
                p.Y = CompressPoint.Y + p.Y * scale;
                p.Z = CompressPoint.Z + p.Z * scale;
            }
        }

        public Boolean WriteBits(UInt32 bitCount, Byte[] bitPtr)
        {
            if (bitCount == 0)
                return true;

            if (bitCount + BitNum > MaxWriteBitNum && !ResizeBits(bitCount + BitNum - MaxWriteBitNum))
                return false;

            var upShift = BitNum & 0x7;
            var downShift = 8 - upShift;

            var sourcePtr = bitPtr;
            var sourceOff = 0;

            var destPtr = GetBuffer();
            var destOff = BitNum >> 3;

            if (downShift >= bitCount)
            {
                var mask = ((1 << (Int32) bitCount) - 1) << (Int32) upShift;

                destPtr[destOff] = (Byte) ((destPtr[destOff] & ~mask) | ((sourcePtr[sourceOff] << (Int32) upShift) & mask));

                BitNum += bitCount;
                return true;
            }

            if (upShift == 0)
            {
                BitNum += bitCount;

                for (; bitCount >= 8; bitCount -= 8)
                    destPtr[destOff++] = sourcePtr[sourceOff++];

                if (bitCount > 0)
                {
                    var mask = (1 << (Int32) bitCount) - 1;
                    destPtr[destOff] = (Byte) ((sourcePtr[sourceOff] & mask) | (destPtr[destOff] & ~mask));
                }

                return true;
            }

            Byte sourceByte;
            var destByte = (Byte) (destPtr[destOff] & (0xFF >> (Int32) downShift));
            var lastMask = (Byte) (0xFF >> (Int32) (7 - ((BitNum + bitCount - 1) & 0x7)));

            BitNum += bitCount;

            for (; bitCount >= 8; bitCount -= 8)
            {
                sourceByte = sourcePtr[sourceOff++];

                destPtr[destOff++] = (Byte) (destByte | (sourceByte << (Int32) upShift));

                destByte = (Byte) (sourceByte >> (Int32) downShift);
            }

            if (bitCount == 0)
            {
                destPtr[destOff] = (Byte) ((destPtr[destOff] & ~lastMask) | (destByte & lastMask));
                return true;
            }

            if (bitCount <= downShift)
            {
                destPtr[destOff] = (Byte) ((destPtr[destOff] & ~lastMask) | ((destByte | (sourcePtr[sourceOff] << (Int32) upShift)) & lastMask));
                return true;
            }

            sourceByte = sourcePtr[sourceOff];

            destPtr[destOff++] = (Byte) (destByte | (sourceByte << (Int32) upShift));
            destPtr[destOff] = (Byte) ((destPtr[destOff] & ~lastMask) | ((sourceByte >> (Int32) downShift) & lastMask));
            return true;
        }

        public Boolean ReadBits(UInt32 bitCount, Byte[] bitPtr)
        {
            if (bitCount == 0)
                return true;

            if (bitCount + BitNum > MaxReadBitNum)
            {
                Error = true;
                return false;
            }

            var sourcePtr = GetBuffer();
            var sourceOff = BitNum >> 3;

            var byteCount = (bitCount + 7) >> 3;

            var destPtr = bitPtr;
            var destOff = 0;

            var downShift = BitNum & 0x7;
            var upShift = 8 - downShift;

            if (downShift == 0)
            {
                while (byteCount-- > 0)
                    destPtr[destOff++] = sourcePtr[sourceOff++];

                BitNum += bitCount;
                return true;
            }

            var sourceByte = (Byte)(sourcePtr[sourceOff] >> (Int32) downShift);
            BitNum += bitCount;

            for (; bitCount >= 8; bitCount -= 8)
            {
                var nextByte = sourcePtr[++sourceOff];

                destPtr[destOff++] = (Byte) (sourceByte | (nextByte << (Int32) upShift));

                sourceByte = (Byte) (nextByte >> (Int32) downShift);
            }

            if (bitCount > 0)
            {
                if (bitCount <= upShift)
                {
                    destPtr[destOff] = sourceByte;
                    return true;
                }
                destPtr[destOff] = (Byte) (sourceByte | (sourcePtr[++sourceOff] << (Int32) upShift));
            }

            return true;
        }

        public Boolean Write(ByteBuffer theBuffer)
        {
            var size = theBuffer.GetBufferSize();
            if (size > 1023)
                return false;

            WriteInt(size, 10);
            return Write(size, theBuffer.GetBuffer());
        }

        public Boolean Read(ByteBuffer theBuffer)
        {
            var size = ReadInt(10);

            theBuffer.Resize(size);

            return Read(size, theBuffer.GetBuffer());
        }

        public Boolean WriteFlag(Boolean flag)
        {
            if (BitNum + 1 > MaxWriteBitNum && !ResizeBits(1))
                return false;

            if (flag)
                Data[BitNum >> 3] |= (Byte) (1 << (Int32) (BitNum & 0x7));
            else
                Data[BitNum >> 3] &= (Byte)~(1 << (Int32) (BitNum & 0x7));

            ++BitNum;
            return flag;
        }

        public Boolean ReadFlag()
        {
            if (BitNum > MaxReadBitNum)
            {
                Error = true;
                return false;
            }

            var mask = 1 << ((Int32) BitNum & 0x7);
            var ret = (Data[BitNum >> 3] & mask) != 0;

            ++BitNum;

            return ret;
        }

        public Boolean Write(Boolean value)
        {
            WriteFlag(value);
            return !Error;
        }

        public Boolean Read(out Boolean value)
        {
            value = ReadFlag();
            return !Error;
        }

        #region Strings

        public void WriteString(String text, Byte maxLen = 255)
        {
            if (text == null)
                text = "";

            var chars = GetCharsForText(text, maxLen);

            Byte j;
            for (j = 0; j < maxLen && StringBuffer[j] == chars[j] && StringBuffer[j] != 0; ++j) { }

            Array.Copy(chars, j, StringBuffer, j, maxLen - j);
            StringBuffer[maxLen] = 0;

            if (WriteFlag(j > 2))
            {
                WriteInt(j, 8);
                WriteHuffBuffer(j, (Byte)(maxLen - j));
            }
            else
                WriteHuffBuffer(0, maxLen);
        }

        private static Byte[] GetCharsForText(String text, Byte maxlen)
        {
            var ret = new Byte[maxlen];
            var txtb = Encoding.UTF8.GetBytes(text);
            Array.Copy(txtb, 0, ret, 0, Math.Min(maxlen, text.Length));
            return ret;
        }

        public void ReadString(out String stringBuf)
        {
            ReadHuffBuffer(out stringBuf, (Byte)(ReadFlag() ? ReadInt(8) : 0));
        }

        private void ReadHuffBuffer(out String stringBuffer, Byte off = 0)
        {
            HuffmanTree.Build();

            UInt32 len;

            if (ReadFlag())
            {
                len = ReadInt(8);

                for (var i = 0; i < len; ++i)
                {
                    var current = HuffmanTree.Root;

                    while (true)
                    {
                        if (!HuffmanTree.IsLeaf(current))
                        {
                            current = ReadFlag() ? current.Right : current.Left;
                            continue;
                        }

                        StringBuffer[i + off] = current.Symbol;
                        break;
                    }
                }
            }
            else
            {
                len = ReadInt(8);

                var buff = new Byte[len];

                Read(len, buff);

                Array.Copy(buff, 0, StringBuffer, off, len);

                StringBuffer[off + len] = 0;
            }

            stringBuffer = Encoding.UTF8.GetString(StringBuffer, 0, (Int32) len + off);
        }

        private void WriteHuffBuffer(Byte off, Byte maxlen)
        {
            if (StringBuffer[off] == 0)
            {
                WriteFlag(false);
                WriteInt(0, 8);
                return;
            }

            HuffmanTree.Build();

            var len = Strlen(StringBuffer, off);
            if (len > maxlen)
                len = maxlen;

            var numBits = 0U;
            for (var i = 0; i < len; ++i)
                numBits += HuffmanTree.Leaves[StringBuffer[off + i]].NumBits;

            var flag = WriteFlag(numBits < (len * 8));
            WriteInt(len, 8);

            if (flag)
            {
                for (var i = 0; i < len; ++i)
                {
                    var leaf = HuffmanTree.Leaves[StringBuffer[off + i]];

                    var code = BitConverter.GetBytes(leaf.Code);

                    WriteBits(leaf.NumBits, code);
                }
            }
            else
            {
                var temp = new Byte[len];

                Array.Copy(StringBuffer, off, temp, 0, len);

                Write(len, temp);
            }
        }

        private static UInt32 Strlen(Byte[] buffer, Byte off)
        {
            var c = 0U;

            while (buffer[off + c] > 0)
                ++c;

            return c;
        }

        #endregion Strings

        public void WriteStringTableEntry(StringTableEntry ste)
        {
            if (StringTable != null)
                StringTable.WriteStringTableEntry(this, ste);
            else
                WriteString(ste.GetString());
        }

        public void ReadStringTableEntry(out StringTableEntry ste)
        {
            if (StringTable != null)
                ste = StringTable.ReadStringTableEntry(this);
            else
            {
                String buf;
                ReadString(out buf);

                ste = new StringTableEntry();
                ste.Set(buf.Contains("\0") ? buf.Substring(0, buf.IndexOf('\0')) : buf);
            }
        }

        public Boolean Write(UInt32 numBytes, Byte[] buffer)
        {
            return WriteBits(numBytes << 3, buffer);
        }

        public Boolean Read(UInt32 numBytes, Byte[] buffer)
        {
            return ReadBits(numBytes << 3, buffer);
        }

        #region TemplatizedReadWrite

        public Boolean Write(Byte value)
        {
            var temp = value;

            for (var i = 0; i < 1; ++i)
            {
                CurrentByte[0] = (Byte) ((temp >> (i * 8)) & 0xFF);

                if (i != 1 - 1)
                    WriteBits(8, CurrentByte);
            }

            return WriteBits(8, CurrentByte);
        }

        public Boolean Read(out Byte value)
        {
            var temp = new Byte[1];

            var success = Read(1, temp);

            value = temp[0];

            return success;
        }

        public Boolean Write(SByte value)
        {
            return Write(1, new [] { (Byte) value });
        }

        public Boolean Read(out SByte value)
        {
            var arr = new Byte[1];

            var success = Read(1, arr);

            value = (SByte) arr[0];

            return success;
        }

        public Boolean Write(UInt16 value)
        {
            return Write(2, BitConverter.GetBytes(value));
        }

        public Boolean Read(out UInt16 value)
        {
            var arr = new Byte[2];

            var success = Read(2, arr);

            value = BitConverter.ToUInt16(arr, 0);

            return success;
        }

        public Boolean Write(Int16 value)
        {
            return Write(2, BitConverter.GetBytes(value));
        }

        public Boolean Read(out Int16 value)
        {
            var arr = new Byte[2];

            var success = Read(2, arr);

            value = BitConverter.ToInt16(arr, 0);

            return success;
        }

        public Boolean Write(UInt32 value)
        {
            return Write(4, BitConverter.GetBytes(value));
        }

        public Boolean Read(out UInt32 value)
        {
            var arr = new Byte[4];

            var success = Read(4, arr);

            value = BitConverter.ToUInt32(arr, 0);

            return success;
        }

        public Boolean Write(Int32 value)
        {
            return Write(4, BitConverter.GetBytes(value));
        }

        public Boolean Read(out Int32 value)
        {
            var arr = new Byte[4];

            var success = Read(4, arr);

            value = BitConverter.ToInt32(arr, 0);

            return success;
        }

        public Boolean Write(UInt64 value)
        {
            return Write(8, BitConverter.GetBytes(value));
        }

        public Boolean Read(out UInt64 value)
        {
            var arr = new Byte[8];

            var success = Read(8, arr);

            value = BitConverter.ToUInt64(arr, 0);

            return success;
        }

        public Boolean Write(Int64 value)
        {
            return Write(8, BitConverter.GetBytes(value));
        }

        public Boolean Read(out Int64 value)
        {
            var arr = new Byte[8];

            var success = Read(8, arr);

            value = BitConverter.ToInt64(arr, 0);

            return success;
        }

        public Boolean Write(Single value)
        {
            return Write(4, BitConverter.GetBytes(value));
        }

        public Boolean Read(out Single value)
        {
            var arr = new Byte[4];

            var success = Read(4, arr);

            value = BitConverter.ToSingle(arr, 0);

            return success;
        }

        public Boolean Write(Double value)
        {
            return Write(8, BitConverter.GetBytes(value));
        }

        public Boolean Read(out Double value)
        {
            var arr = new Byte[8];

            var success = Read(8, arr);

            value = BitConverter.ToDouble(arr, 0);

            return success;
        }

        #endregion TemplatizedReadWrite

        public Boolean SetBit(UInt32 bitCount, Boolean set)
        {
            if (bitCount >= MaxWriteBitNum && !ResizeBits(bitCount - MaxWriteBitNum + 1))
                return false;

            if (set)
                GetBuffer()[bitCount >> 3] |= (Byte) (1 << ((Int32) bitCount & 0x7));
            else
                GetBuffer()[bitCount >> 3] &= (Byte)~(1 << ((Int32) bitCount & 0x7));

            return true;
        }

        public Boolean TestBit(UInt32 bitCount)
        {
            return (GetBuffer()[bitCount >> 3] & (1 << ((Int32) bitCount & 0x7))) != 0;
        }

        public Boolean IsFull()
        {
            return BitNum > (GetBufferSize() << 3);
        }

        public Boolean IsValid()
        {
            return !Error;
        }

        public void HashAndEncrypt(UInt32 hashDigestSize, UInt32 encryptStartOffset, SymmetricCipher theCipher)
        {
            var digestStart = GetBytePosition();
            SetBytePosition(digestStart);

            var hash = new SHA256Managed().ComputeHash(Data, 0, (Int32) digestStart);

            Write(hashDigestSize, hash);

            theCipher.Encrypt(GetBuffer(), encryptStartOffset, GetBuffer(), encryptStartOffset, GetBytePosition() - encryptStartOffset);
        }

        public Boolean DecryptAndCheckHash(UInt32 hashDigestSize, UInt32 decryptStartOffset, SymmetricCipher theCipher)
        {
            var bufferSize = GetBufferSize();
            var buffer = GetBuffer();

            if (bufferSize < decryptStartOffset + hashDigestSize)
                return false;

            theCipher.Decrypt(buffer, decryptStartOffset, buffer, decryptStartOffset, bufferSize - decryptStartOffset);

            var hash = new SHA256Managed().ComputeHash(buffer, 0, (Int32)(bufferSize - hashDigestSize));

            var ret = Memcmp(buffer, bufferSize - hashDigestSize, hash, 0U, hashDigestSize);
            if (ret)
                Resize(bufferSize - hashDigestSize);

            return ret;
        }

        private static Boolean Memcmp(Byte[] a, UInt32 offsetA, Byte[] b, UInt32 offsetB, UInt32 length)
        {
            for (var i = 0U; i < length; ++i)
                if (a[offsetA + i] != b[offsetB + i])
                    return false;

            return true;
        }
    }
}
