using System;

namespace TNL.NET.Entities
{
    using Utils;

    // TODO: System.Security.Cryptography.Rijndael / System.Security.Cryptography.RijndaelManaged

    public class SymmetricCipher
    {
        public const Int32 BlockSize = 16;
        public const Int32 KeySize = 16;

        private UInt32[] _counter = new UInt32[4];
        private UInt32[] _initVector = new UInt32[16];
        private Byte[] _pad = new Byte[16];
        private Key _symmetricKey = new Key();
        private UInt32 _padLen;

        public SymmetricCipher(ByteBuffer theByteBuffer)
        {
            if (theByteBuffer.GetBufferSize() != KeySize * 2)
            {
                var buffer = new Byte[KeySize];

                throw new NotImplementedException();

                Array.Copy(buffer, _initVector, BlockSize);
            }
            else
            {
                throw new NotImplementedException();

                Array.Copy(theByteBuffer.GetBuffer(), KeySize, _initVector, 0, BlockSize);
            }
            Array.Copy(_initVector, _counter, BlockSize);
            _padLen = 0;
        }

        public SymmetricCipher(Byte[] symmetricKey, Byte[] initVector)
        {
            Array.Copy(initVector, _initVector, BlockSize);
            Array.Copy(initVector, _counter, BlockSize); // Invalid Write in the Original TNL code i guess
            
            throw new NotImplementedException();

            _padLen = 0;
        }

        public void SetupCounter(UInt32 counterValue1, UInt32 counterValue2, UInt32 counterValue3, UInt32 counterValue4)
        {
            _counter[0] = _initVector[0] + counterValue1;
            _counter[1] = _initVector[1] + counterValue2;
            _counter[2] = _initVector[2] + counterValue3;
            _counter[3] = _initVector[3] + counterValue4;

            throw new NotImplementedException();

            _padLen = 0;
        }

        public void Encrypt(Byte[] plainText, UInt32 plainTextOffset, Byte[] cipherText, UInt32 cipherTextOffset, UInt32 len)
        {
            while (len-- > 0)
            {
                if (_padLen == 16)
                {
                    throw new NotImplementedException();
                    _padLen = 0;
                }

                var encryptedChar = (Byte)(plainText[plainTextOffset++] ^ _pad[_padLen]);
                _pad[_padLen++] = cipherText[cipherTextOffset++] = encryptedChar;
            }
            
        }

        public void Decrypt(Byte[] plainText, UInt32 plainTextOffset, Byte[] cipherText, UInt32 cipherTextOffset, UInt32 len)
        {
            while (len-- > 0)
            {
                if (_padLen == BlockSize)
                {
                    throw new NotImplementedException();
                    _padLen = 0;
                }

                var encryptedChar = cipherText[cipherTextOffset++];
                plainText[plainTextOffset++] = (Byte) (encryptedChar ^ _pad[_padLen]);
                _pad[_padLen++] = encryptedChar;
            }
            
        }

        private class Key
        {
            public UInt32[] Ek = new UInt32[64];
            public UInt32[] Dk = new UInt32[64];

            public Int32 Nr { get; set; }
        }
    }
}
