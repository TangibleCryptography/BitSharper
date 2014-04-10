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

using System.Linq;
using System.Text;
using NUnit.Framework;
using Org.BouncyCastle.Math;

namespace BitSharper.Test
{
    [TestFixture]
    public class Base58Test
    {
        [Test]
        public void TestEncode()
        {
            var testbytes = Encoding.UTF8.GetBytes("Hello World");
            Assert.AreEqual("JxF12TrwUP45BMd", Base58.Encode(testbytes));

            var bi = BigInteger.ValueOf(3471844090);
            Assert.AreEqual("16Ho7Hs", Base58.Encode(bi.ToByteArray()));
        }

        [Test]
        public void TestDecode()
        {
            var testbytes = Encoding.UTF8.GetBytes("Hello World");
            var actualbytes = Base58.Decode("JxF12TrwUP45BMd");
            Assert.IsTrue(testbytes.SequenceEqual(actualbytes), Encoding.UTF8.GetString(actualbytes, 0, actualbytes.Length));

            try
            {
                Base58.Decode("This isn't valid base58");
                Assert.Fail();
            }
            catch (AddressFormatException)
            {
            }

            Base58.DecodeChecked("4stwEBjT6FYyVV");

            // Now check we can correctly decode the case where the high bit of the first byte is not zero, so BigInteger
            // sign extends. Fix for a bug that stopped us parsing keys exported using Sipa's patch.
            Base58.DecodeChecked("93VYUMzRG9DdbRP72uQXjaWibbQwygnvaCu9DumcqDjGybD864T");
        }
    }
}