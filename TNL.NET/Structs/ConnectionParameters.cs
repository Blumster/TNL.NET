using System;
using System.Collections.Generic;
using System.Net;

namespace TNL.NET.Structs
{
    using Entities;
    using Utils;

    public class ConnectionParameters
    {
        public Boolean IsArranged;
        public Boolean UsingCrypto;
        public Boolean PuzzleRetried;
        public Nonce Nonce;
        public Nonce ServerNonce;
        public UInt32 PuzzleDifficulty;
        public UInt32 PuzzleSolution;
        public UInt32 ClientIdentity;
        public AsymmetricKey PublicKey;
        public AsymmetricKey PrivateKey;
        public Certificate Certificate;
        public ByteBuffer SharedSecret;
        public Boolean RequestKeyExchange;
        public Boolean RequestCertificate;
        public Byte[] SymmetricKey = new Byte[SymmetricCipher.KeySize];
        public Byte[] InitVector = new Byte[SymmetricCipher.KeySize];
        public List<IPEndPoint> PossibleAddresses;
        public Boolean IsInitiator;
        public Boolean IsLocal;
        public ByteBuffer ArrangedSecret;
        public Boolean DebugObjectSizes;

        public ConnectionParameters()
        {
            IsInitiator = false;
            PuzzleRetried = false;
            UsingCrypto = false;
            IsArranged = false;
            DebugObjectSizes = false;     
            IsLocal = false;

            Nonce = new Nonce();
            ServerNonce = new Nonce();
        }
    };
}
