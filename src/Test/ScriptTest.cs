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
using Org.BouncyCastle.Utilities.Encoders;

namespace BitSharper.Test
{
    [TestFixture]
    public class ScriptTest
    {
        // From tx 05e04c26c12fe408a3c1b71aa7996403f6acad1045252b1c62e055496f4d2cb1 on the testnet.

        private const string _sigProg = "473    04402202b4da291cc39faf8433911988f9f49fc5c995812ca2f94db61468839c228c3e90220628bff3ff32ec95825092fa051cba28558a981fcf59ce184b14f2e215e69106701410414b38f4be3bb9fa0f4f32b74af07152b2f2f630bc02122a491137b6c523e46f18a0d5034418966f93dfc37cc3739ef7b2007213a302b7fba161557f4ad644a1c";

        private const string _pubkeyProg = "76a91433e81a941e64cda12c6a299ed322ddbdd03f8d0e88ac";

        private static readonly NetworkParameters _params = NetworkParameters.TestNet();

        [Test]
        public void TestScriptSig()
        {
            var sigProgBytes = Hex.Decode(_sigProg);
            var script = new Script(_params, sigProgBytes, 0, sigProgBytes.Length);
            // Test we can extract the from address.
            var hash160 = Utils.Sha256Hash160(script.PubKey);
            var a = new Address(_params, hash160);
            Assert.AreEqual("mkFQohBpy2HDXrCwyMrYL5RtfrmeiuuPY2", a.ToString());
        }

        [Test]
        public void TestScriptPubKey()
        {
            // Check we can extract the to address
            var pubkeyBytes = Hex.Decode(_pubkeyProg);
            var pubkey = new Script(_params, pubkeyBytes, 0, pubkeyBytes.Length);
            var toAddr = new Address(_params, pubkey.PubKeyHash);
            Assert.AreEqual("mkFQohBpy2HDXrCwyMrYL5RtfrmeiuuPY2", toAddr.ToString());
        }

        [Test]
        public void TestIp()
        {
            var bytes = Hex.Decode("41043e96222332ea7848323c08116dddafbfa917b8e37f0bdf63841628267148588a09a43540942d58d49717ad3fabfe14978cf4f0a8b84d2435dad16e9aa4d7f935ac");
            var s = new Script(_params, bytes, 0, bytes.Length);
            Assert.IsTrue(s.IsSentToIp);
        }
    }
}