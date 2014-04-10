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
    public class AddressTest
    {
        private static readonly NetworkParameters _testParams = NetworkParameters.TestNet();
        private static readonly NetworkParameters _prodParams = NetworkParameters.ProdNet();

        [Test]
        public void TestStringification()
        {
            // Test a testnet address.
            var a = new Address(_testParams, Hex.Decode("fda79a24e50ff70ff42f7d89585da5bd19d9e5cc"));
            Assert.AreEqual("n4eA2nbYqErp7H6jebchxAN59DmNpksexv", a.ToString());

            var b = new Address(_prodParams, Hex.Decode("4a22c3c4cbb31e4d03b15550636762bda0baf85a"));
            Assert.AreEqual("17kzeh4N8g49GFvdDzSf8PjaPfyoD1MndL", b.ToString());
        }

        [Test]
        public void TestDecoding()
        {
            var a = new Address(_testParams, "n4eA2nbYqErp7H6jebchxAN59DmNpksexv");
            Assert.AreEqual("fda79a24e50ff70ff42f7d89585da5bd19d9e5cc", Utils.BytesToHexString(a.Hash160));

            var b = new Address(_prodParams, "17kzeh4N8g49GFvdDzSf8PjaPfyoD1MndL");
            Assert.AreEqual("4a22c3c4cbb31e4d03b15550636762bda0baf85a", Utils.BytesToHexString(b.Hash160));
        }
    }
}