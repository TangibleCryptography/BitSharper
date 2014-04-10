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

using System;
using NUnit.Framework;

namespace BitSharper.Test
{
    [TestFixture]
    public class UtilsTest
    {
        [Test]
        public void TestToNanoCoins()
        {
            // String version
            Assert.AreEqual(Utils.Cent, Utils.ToNanoCoins("0.01"));
            Assert.AreEqual(Utils.Cent, Utils.ToNanoCoins("1E-2"));
            Assert.AreEqual(Utils.Coin + Utils.Cent, Utils.ToNanoCoins("1.01"));
            try
            {
                Utils.ToNanoCoins("2E-20");
                Assert.Fail("should not have accepted fractional nanocoins");
            }
            catch (ArithmeticException)
            {
            }

            // int version
            Assert.AreEqual(Utils.Cent, Utils.ToNanoCoins(0, 1));
        }

        [Test]
        public void TestFormatting()
        {
            Assert.AreEqual("1.23", Utils.BitcoinValueToFriendlyString(Utils.ToNanoCoins(1, 23)));
            Assert.AreEqual("-1.23", Utils.BitcoinValueToFriendlyString(-(long) Utils.ToNanoCoins(1, 23)));
        }
    }
}