/*
 * Copyright 2011 Micheal Swiggs
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

using System.Linq;
using BitSharper.Discovery;
using NUnit.Framework;

namespace BitSharper.Test.Discovery
{
    [TestFixture]
    public class SeedPeersTest
    {
        [Test]
        public void GetPeerOne()
        {
            var seedPeers = new SeedPeers(NetworkParameters.ProdNet());
            Assert.IsNotNull(seedPeers.GetPeer());
        }

        [Test]
        public void GetPeerAll()
        {
            var seedPeers = new SeedPeers(NetworkParameters.ProdNet());
            for (var i = 0; i < SeedPeers.SeedAddrs.Length; ++i)
            {
                Assert.IsNotNull(seedPeers.GetPeer(), "Failed on index: " + i);
            }
            Assert.IsNull(seedPeers.GetPeer());
        }

        [Test]
        public void GetPeersLength()
        {
            var seedPeers = new SeedPeers(NetworkParameters.ProdNet());
            var addresses = seedPeers.GetPeers();
            Assert.AreEqual(addresses.Count(), SeedPeers.SeedAddrs.Length);
        }
    }
}