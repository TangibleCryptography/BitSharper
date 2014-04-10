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

using System.Net;
using NUnit.Framework;
using Org.BouncyCastle.Utilities.Encoders;

namespace BitSharper.Test
{
    [TestFixture]
    public class PeerAddressTest
    {
        [Test]
        public void TestPeerAddressRoundtrip()
        {
            // copied verbatim from https://en.bitcoin.it/wiki/Protocol_specification#Network_address
            const string fromSpec = "010000000000000000000000000000000000ffff0a000001208d";
            var pa = new PeerAddress(NetworkParameters.ProdNet(), Hex.Decode(fromSpec), 0, 0);
            var reserialized = Utils.BytesToHexString(pa.BitcoinSerialize());
            Assert.AreEqual(reserialized, fromSpec);
        }

        [Test]
        public void TestBitcoinSerialize()
        {
            var pa = new PeerAddress(IPAddress.Loopback, 8333, 0);
            Assert.AreEqual("000000000000000000000000000000000000ffff7f000001208d",
                            Utils.BytesToHexString(pa.BitcoinSerialize()));
        }
    }
}