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

namespace BitSharper.Test
{
    [TestFixture]
    public class VarIntTest
    {
        [Test]
        public void TestBytes()
        {
            var a = new VarInt(10);
            Assert.AreEqual(1, a.SizeInBytes);
            Assert.AreEqual(1, a.Encode().Length);
            Assert.AreEqual(10UL, new VarInt(a.Encode(), 0).Value);
        }

        [Test]
        public void TestShorts()
        {
            var a = new VarInt(64000);
            Assert.AreEqual(3, a.SizeInBytes);
            Assert.AreEqual(3, a.Encode().Length);
            Assert.AreEqual(64000UL, new VarInt(a.Encode(), 0).Value);
        }

        [Test]
        public void TestInts()
        {
            var a = new VarInt(0xAABBCCDD);
            Assert.AreEqual(5, a.SizeInBytes);
            Assert.AreEqual(5, a.Encode().Length);
            var bytes = a.Encode();
            Assert.AreEqual(0xAABBCCDD, new VarInt(bytes, 0).Value);
        }

        [Test]
        public void TestLong()
        {
            var a = new VarInt(0xCAFEBABEDEADBEEF);
            Assert.AreEqual(9, a.SizeInBytes);
            Assert.AreEqual(9, a.Encode().Length);
            var bytes = a.Encode();
            Assert.AreEqual(0xCAFEBABEDEADBEEF, new VarInt(bytes, 0).Value);
        }
    }
}