/*
 * Copyright 2011 Noa Resare
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

using System.IO;
using NUnit.Framework;
using Org.BouncyCastle.Utilities.Encoders;

namespace BitSharper.Test
{
    [TestFixture]
    public class BitcoinSerializerTest
    {
        [Test]
        public void TestVersion()
        {
            var bs = new BitcoinSerializer(NetworkParameters.ProdNet(), false);
            // the actual data from https://en.bitcoin.it/wiki/Protocol_specification#version
            using (var bais = new MemoryStream(Hex.Decode("f9beb4d976657273696f6e0000000000550000009" +
                                                          "c7c00000100000000000000e615104d00000000010000000000000000000000000000000000ffff0a000001daf6010000" +
                                                          "000000000000000000000000000000ffff0a000002208ddd9d202c3ab457130055810100")))
            {
                var vm = (VersionMessage) bs.Deserialize(bais);
                Assert.AreEqual(31900U, vm.ClientVersion);
                Assert.AreEqual(1292899814UL, vm.Time);
                Assert.AreEqual(98645U, vm.BestHeight);
            }
        }

        [Test]
        public void TestVerack()
        {
            var bs = new BitcoinSerializer(NetworkParameters.ProdNet(), false);
            // the actual data from https://en.bitcoin.it/wiki/Protocol_specification#verack
            using (var bais = new MemoryStream(Hex.Decode("f9beb4d976657261636b00000000000000000000")))
            {
                bs.Deserialize(bais);
            }
        }

        [Test]
        public void TestAddr()
        {
            var bs = new BitcoinSerializer(NetworkParameters.ProdNet(), true);
            // the actual data from https://en.bitcoin.it/wiki/Protocol_specification#addr
            using (var bais = new MemoryStream(Hex.Decode("f9beb4d96164647200000000000000001f000000" +
                                                          "ed52399b01e215104d010000000000000000000000000000000000ffff0a000001208d")))
            {
                var a = (AddressMessage) bs.Deserialize(bais);
                Assert.AreEqual(1, a.Addresses.Count);
                var pa = a.Addresses[0];
                Assert.AreEqual(8333, pa.Port);
                Assert.AreEqual("10.0.0.1", pa.Addr.ToString());
            }
        }
    }
}