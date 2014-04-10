/*
 * Copyright 2011 Google Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *    http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using NUnit.Framework;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Utilities.Encoders;

namespace BitSharper.Test
{
    [TestFixture]
    public class EcKeyTest
    {
        [Test]
        public void TestSignatures()
        {
            // Test that we can construct an ECKey from a private key (deriving the public from the private), then signing
            // a message with it.
            var privkey = new BigInteger(1, Hex.Decode("180cb41c7c600be951b5d3d0a7334acc7506173875834f7a6c4c786a28fcbb19"));
            var key = new EcKey(privkey);
            var message = new byte[32]; // All zeroes.
            var output = key.Sign(message);
            Assert.IsTrue(key.Verify(message, output));

            // Test interop with a signature from elsewhere.
            var sig = Hex.Decode("3046022100dffbc26774fc841bbe1c1362fd643609c6e42dcb274763476d87af2c0597e89e022100c59e3c13b96b316cae9fa0ab0260612c7a133a6fe2b3445b6bf80b3123bf274d");
            Assert.IsTrue(key.Verify(message, sig));
        }

        [Test]
        public void TestAsn1Roundtrip()
        {
            var privKeyAsn1 = Hex.Decode("3082011302010104205c0b98e524ad188ddef35dc6abba13c34a351a05409e5d285403718b93336a4aa081a53081a2020101302c06072a8648ce3d0101022100fffffffffffffffffffffffffffffffffffffffffffffffffffffffefffffc2f300604010004010704410479be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798483ada7726a3c4655da4fbfc0e1108a8fd17b448a68554199c47d08ffb10d4b8022100fffffffffffffffffffffffffffffffebaaedce6af48a03bbfd25e8cd0364141020101a144034200042af7a2aafe8dafd7dc7f9cfb58ce09bda7dce28653ab229b98d1d3d759660c672dd0db18c8c2d76aa470448e876fc2089ab1354c01a6e72cefc50915f4a963ee");
            var decodedKey = EcKey.FromAsn1(privKeyAsn1);

            // Now re-encode and decode the ASN.1 to see if it is equivalent (it does not produce the exact same byte
            // sequence, some integers are padded now).
            var roundtripKey = EcKey.FromAsn1(decodedKey.ToAsn1());

            byte[] message;
            foreach (var key in new[] {decodedKey, roundtripKey})
            {
                message = Utils.ReverseBytes(Hex.Decode("11da3761e86431e4a54c176789e41f1651b324d240d599a7067bee23d328ec2a"));
                var output = key.Sign(message);
                Assert.IsTrue(key.Verify(message, output));

                output = Hex.Decode("304502206faa2ebc614bf4a0b31f0ce4ed9012eb193302ec2bcaccc7ae8bb40577f47549022100c73a1a1acc209f3f860bf9b9f5e13e9433db6f8b7bd527a088a0e0cd0a4c83e9");
                Assert.IsTrue(key.Verify(message, output));
            }

            // Try to sign with one key and verify with the other.
            message = Utils.ReverseBytes(Hex.Decode("11da3761e86431e4a54c176789e41f1651b324d240d599a7067bee23d328ec2a"));
            Assert.IsTrue(roundtripKey.Verify(message, decodedKey.Sign(message)));
            Assert.IsTrue(decodedKey.Verify(message, roundtripKey.Sign(message)));
        }

        [Test]
        public void Base58Encoding()
        {
            const string addr = "mqAJmaxMcG5pPHHc3H3NtyXzY7kGbJLuMF";
            const string privkey = "92shANodC6Y4evT5kFzjNFQAdjqTtHAnDTLzqBBq4BbKUPyx6CD";
            var key = new DumpedPrivateKey(NetworkParameters.TestNet(), privkey).Key;
            Assert.AreEqual(privkey, key.GetPrivateKeyEncoded(NetworkParameters.TestNet()).ToString());
            Assert.AreEqual(addr, key.ToAddress(NetworkParameters.TestNet()).ToString());
        }

        [Test]
        public void Base58EncodingLeadingZero()
        {
            const string privkey = "91axuYLa8xK796DnBXXsMbjuc8pDYxYgJyQMvFzrZ6UfXaGYuqL";
            var key = new DumpedPrivateKey(NetworkParameters.TestNet(), privkey).Key;
            Assert.AreEqual(privkey, key.GetPrivateKeyEncoded(NetworkParameters.TestNet()).ToString());
            Assert.AreEqual(0, key.GetPrivKeyBytes()[0]);
        }

        [Test]
        public void Base58EncodingStress()
        {
            // Replace the loop bound with 1000 to get some keys with leading zero byte
            for (var i = 0; i < 20; i++)
            {
                var key = new EcKey();
                var key1 = new DumpedPrivateKey(NetworkParameters.TestNet(),
                                                key.GetPrivateKeyEncoded(NetworkParameters.TestNet()).ToString()).Key;
                Assert.AreEqual(Utils.BytesToHexString(key.GetPrivKeyBytes()),
                                Utils.BytesToHexString(key1.GetPrivKeyBytes()));
            }
        }
    }
}