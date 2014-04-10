/*
 * Copyright 2011 John Sample
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

using BitSharper.Discovery;
using NUnit.Framework;

namespace BitSharper.Test.Discovery
{
    [TestFixture]
    public class IrcDiscoveryTest
    {
        // TODO: Inject a mock IRC server and more thoroughly exercise this class.

        [Test]
        public void TestParseUserList()
        {
            // Test some random addresses grabbed from the channel.
            var userList = new[] {"x201500200", "u4stwEBjT6FYyVV", "u5BKEqDApa8SbA7"};

            var addresses = IrcDiscovery.ParseUserList(userList);

            // Make sure the "x" address is excluded.
            Assert.AreEqual(2, addresses.Count, "Too many addresses.");

            var ips = new[] {"69.4.98.82:8333", "74.92.222.129:8333"};

            for (var i = 0; i < addresses.Count; i++)
            {
                Assert.AreEqual(ips[i], addresses[i].ToString(), "IPs decoded improperly");
            }
        }
    }
}