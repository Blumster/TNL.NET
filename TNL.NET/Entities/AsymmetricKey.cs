using System;
using System.CodeDom.Compiler;
using System.Security.Cryptography;

namespace TNL.NET.Entities
{
    using Utils;

    public enum KeyType
    {
        KeyTypePrivate,
        KeyTypePublic
    }

    public class AsymmetricKey : ByteBuffer
    {
        public const UInt32 StaticCryptoBufferSize = 2048U;

        private static Byte[] _staticCryptoBuffer = new byte[StaticCryptoBufferSize];

        private Byte[] KeyData { get; set; }
        private UInt32 KeySize { get; set; }
        private Boolean PHasPrivateKey { get; set; }
        private ByteBuffer PublicKey { get; set; }
        private ByteBuffer PrivateKey { get; set; }
        private Boolean PIsValid { get; set; }

        public AsymmetricKey(Byte[] buffer, UInt32 bufferSize)
        {
            Load(new ByteBuffer(buffer, bufferSize));
        }

        public AsymmetricKey(BitStream stream)
        {
            var theBuffer = new ByteBuffer();

            stream.Read(theBuffer);

            Load(theBuffer);
        }

        public AsymmetricKey(UInt32 keySize)
        {
            PIsValid = false;

            KeySize = keySize;

            throw new NotImplementedException();
        }

        private void Load(ByteBuffer theBuffer)
        {
            PIsValid = false;
            PHasPrivateKey = theBuffer.GetBuffer()[0] == (Byte) KeyType.KeyTypePrivate;

            var bufferSize = theBuffer.GetBufferSize();
            if (bufferSize < 5)
                return;

            var temp = new Byte[4];
            Array.Copy(theBuffer.GetBuffer(), 1, temp, 0, 4);
            Array.Reverse(temp);

            KeySize = BitConverter.ToUInt32(temp, 0);

            throw new NotImplementedException();
        }

        public ByteBuffer GetPublicKey()
        {
            return PublicKey;
        }

        public ByteBuffer GetPrivateKey()
        {
            return PrivateKey;
        }

        public Boolean HasPrivateKey()
        {
            return PHasPrivateKey;
        }

        public Boolean IsValid()
        {
            return PIsValid;
        }

        public ByteBuffer ComputeSharedSecretKey(AsymmetricKey publicKey)
        {
            if (publicKey.GetKeySize() != GetKeySize() || !PHasPrivateKey)
                return null;

            throw new NotImplementedException();

            var hash = new SHA256Managed().ComputeHash(_staticCryptoBuffer, 0, (Int32) StaticCryptoBufferSize);

            return new ByteBuffer(hash, 32);
        }

        public UInt32 GetKeySize()
        {
            return KeySize;
        }

        public ByteBuffer HashAndSign(ByteBuffer theByteBuffer)
        {
            var hash = new SHA256Managed().ComputeHash(theByteBuffer.GetBuffer(), 0, (Int32)theByteBuffer.GetBufferSize());

            var outLen = StaticCryptoBufferSize;

            throw new NotImplementedException();

            return new ByteBuffer(_staticCryptoBuffer, outLen);
        }

        public bool VerifySignature(ByteBuffer theByteBuffer, ByteBuffer theSignature)
        {
            var hash = new SHA256Managed().ComputeHash(theByteBuffer.GetBuffer(), 0, (Int32) theByteBuffer.GetBufferSize());

            var stat = 0;

            throw new NotImplementedException();

            return stat != 0;
        }
    }
}
