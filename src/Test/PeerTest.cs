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

using System.Collections.Generic;
using System.IO;
using System.Net;
using BitSharper.Store;
using BitSharper.Test.Moq;
using Moq;
using NUnit.Framework;

namespace BitSharper.Test
{
    [TestFixture]
    public class PeerTest
    {
        private Peer _peer;
        private Mock<NetworkConnection> _control;
        private NetworkConnection _conn;
        private NetworkParameters _unitTestParams;
        private MemoryBlockStore _blockStore;
        private BlockChain _blockChain;

        [SetUp]
        public void SetUp()
        {
            _unitTestParams = NetworkParameters.UnitTests();
            _blockStore = new MemoryBlockStore(_unitTestParams);
            _blockChain = new BlockChain(_unitTestParams, new Wallet(_unitTestParams), _blockStore);
            var address = new PeerAddress(IPAddress.Loopback);
            _control = new Mock<NetworkConnection>();
            _conn = _control.Object;
            _peer = new Peer(_unitTestParams, address, _blockChain);
            _peer.Connection = _conn;
        }

        // Check that the connection is shut down if there's a read error and the exception is propagated.
        [Test]
        public void TestRunException()
        {
            _control.Setup(x => x.ReadMessage()).Throws(new IOException("done")).Verifiable();
            _control.Setup(x => x.Shutdown()).Verifiable();

            try
            {
                _peer.Run();
                Assert.Fail("did not throw");
            }
            catch (PeerException e)
            {
                // expected
                Assert.IsTrue(e.InnerException is IOException);
            }

            _control.Verify();

            _control.Setup(x => x.ReadMessage()).Throws(new IOException("done")).Verifiable();
            _control.Setup(x => x.Shutdown()).Verifiable();

            try
            {
                _peer.Run();
                Assert.Fail("did not throw");
            }
            catch (PeerException e)
            {
                // expected
                Assert.IsTrue(e.InnerException is IOException);
            }

            _control.Verify();
        }

        // Check that it runs through the event loop and shut down correctly
        [Test]
        public void TestRunNormal()
        {
            _control.Setup(x => x.ReadMessage()).Returns(ReadFinalMessage).Verifiable();
            RunPeerAndVerify();
        }

        // Check that when we receive a block that does not connect to our chain, we send a
        // getblocks to fetch the intermediates.
        [Test]
        public void TestRunUnconnectedBlock()
        {
            var b1 = TestUtils.CreateFakeBlock(_unitTestParams, _blockStore).Block;
            _blockChain.Add(b1);

            var prev = TestUtils.MakeSolvedTestBlock(_unitTestParams, _blockStore);
            var block = TestUtils.MakeSolvedTestBlock(_unitTestParams, prev);

            _control.Setup(x => x.ReadMessage()).Returns(() => block, ReadFinalMessage).Verifiable();

            var message = CaptureGetBlocksMessage();

            RunPeerAndVerify();

            var expectedLocator = new List<Sha256Hash>();
            expectedLocator.Add(b1.Hash);
            expectedLocator.Add(_unitTestParams.GenesisBlock.Hash);

            Assert.AreEqual(message.Value.Locator, expectedLocator);
            Assert.AreEqual(message.Value.StopHash, block.Hash);
        }

        // Check that an inventory tickle is processed correctly
        [Test]
        public void TestRunInvTickle()
        {
            var b1 = TestUtils.CreateFakeBlock(_unitTestParams, _blockStore).Block;
            _blockChain.Add(b1);

            var prev = TestUtils.MakeSolvedTestBlock(_unitTestParams, _blockStore);
            var block = TestUtils.MakeSolvedTestBlock(_unitTestParams, prev);

            var inv = new InventoryMessage(_unitTestParams);
            var item = new InventoryItem(InventoryItem.ItemType.Block, block.Hash);
            inv.AddItem(item);

            _control.Setup(x => x.ReadMessage()).Returns(() => block, () => inv, ReadFinalMessage).Verifiable();

            var message = CaptureGetBlocksMessage();

            RunPeerAndVerify();

            var expectedLocator = new List<Sha256Hash>();
            expectedLocator.Add(b1.Hash);
            expectedLocator.Add(_unitTestParams.GenesisBlock.Hash);

            Assert.AreEqual(message.Value.Locator, expectedLocator);
            Assert.AreEqual(message.Value.StopHash, block.Hash);
        }

        // Check that inventory message containing a block is processed correctly
        [Test]
        public void TestRunInvBlock()
        {
            var b1 = TestUtils.CreateFakeBlock(_unitTestParams, _blockStore).Block;
            _blockChain.Add(b1);

            var prev = TestUtils.MakeSolvedTestBlock(_unitTestParams, _blockStore);
            var b2 = TestUtils.MakeSolvedTestBlock(_unitTestParams, prev);
            var b3 = TestUtils.MakeSolvedTestBlock(_unitTestParams, b2);

            _control.Setup(x => x.WriteMessage(It.IsAny<Message>())).Verifiable();

            var inv = new InventoryMessage(_unitTestParams);
            var item = new InventoryItem(InventoryItem.ItemType.Block, b3.Hash);
            inv.AddItem(item);

            _control.Setup(x => x.ReadMessage()).Returns(() => b2, () => inv, ReadFinalMessage).Verifiable();

            var message = CaptureGetDataMessage();

            RunPeerAndVerify();

            var items = message.Value.Items;
            Assert.AreEqual(1, items.Count);
            Assert.AreEqual(b3.Hash, items[0].Hash);
            Assert.AreEqual(InventoryItem.ItemType.Block, items[0].Type);
        }

        // Check that it starts downloading the block chain correctly
        [Test]
        public void TestStartBlockChainDownload()
        {
            var b1 = TestUtils.CreateFakeBlock(_unitTestParams, _blockStore).Block;
            _blockChain.Add(b1);

            _control.Setup(x => x.VersionMessage).Returns(new VersionMessage(_unitTestParams, 100)).Verifiable();

            var eventFired = false;
            _peer.ChainDownloadStarted +=
                (sender, e) =>
                {
                    Assert.AreEqual(e.BlocksLeft, 99);
                    eventFired = true;
                };

            var message = CaptureGetBlocksMessage();

            _peer.StartBlockChainDownload();
            _control.Verify();

            var expectedLocator = new List<Sha256Hash>();
            expectedLocator.Add(b1.Hash);
            expectedLocator.Add(_unitTestParams.GenesisBlock.Hash);

            Assert.AreEqual(message.Value.Locator, expectedLocator);
            Assert.AreEqual(message.Value.StopHash, Sha256Hash.ZeroHash);
            Assert.IsTrue(eventFired);
        }

        [Test]
        public void TestGetBlock()
        {
            var b1 = TestUtils.CreateFakeBlock(_unitTestParams, _blockStore).Block;
            _blockChain.Add(b1);

            var prev = TestUtils.MakeSolvedTestBlock(_unitTestParams, _blockStore);
            var b2 = TestUtils.MakeSolvedTestBlock(_unitTestParams, prev);

            var message = CaptureGetDataMessage();

            _control.Setup(x => x.ReadMessage()).Returns(() => b2, ReadFinalMessage).Verifiable();

            _control.Setup(x => x.Shutdown()).Verifiable();

            var resultFuture = _peer.BeginGetBlock(b2.Hash, null, null);
            _peer.Run();

            Assert.AreEqual(b2.Hash, _peer.EndGetBlock(resultFuture).Hash);

            _control.Verify();

            var expectedLocator = new List<Sha256Hash>();
            expectedLocator.Add(b1.Hash);
            expectedLocator.Add(_unitTestParams.GenesisBlock.Hash);

            var items = message.Value.Items;
            Assert.AreEqual(1, items.Count);
            Assert.AreEqual(b2.Hash, items[0].Hash);
            Assert.AreEqual(InventoryItem.ItemType.Block, items[0].Type);
        }

        // Check that the next block on the chain is processed correctly and that the listener is notified
        [Test]
        public void TestRunNewBlock()
        {
            _control.Setup(x => x.ReadMessage()).Returns(() => TestUtils.MakeSolvedTestBlock(_unitTestParams, _blockStore), ReadFinalMessage).Verifiable();
            _control.Setup(x => x.VersionMessage).Returns(new VersionMessage(_unitTestParams, 100)).Verifiable();
            var eventFired = false;
            _peer.BlocksDownloaded +=
                (sender, e) =>
                {
                    Assert.AreEqual(e.BlocksLeft, 99);
                    eventFired = true;
                };
            RunPeerAndVerify();
            Assert.IsTrue(eventFired);
        }

        /// <exception cref="IOException"/>
        private Capture<GetBlocksMessage> CaptureGetBlocksMessage()
        {
            var message = new Capture<GetBlocksMessage>();
            _control.Setup(x => x.WriteMessage(It.IsAny<GetBlocksMessage>())).Callback<Message>(arg => message.Value = (GetBlocksMessage) arg).Verifiable();
            return message;
        }

        /// <exception cref="IOException"/>
        private Capture<GetDataMessage> CaptureGetDataMessage()
        {
            var message = new Capture<GetDataMessage>();
            _control.Setup(x => x.WriteMessage(It.IsAny<GetDataMessage>())).Callback<Message>(arg => message.Value = (GetDataMessage) arg).Verifiable();
            return message;
        }

        // Stage a disconnect, replay the mocks, run and verify
        /// <exception cref="IOException"/>
        /// <exception cref="ProtocolException"/>
        /// <exception cref="PeerException"/>
        private void RunPeerAndVerify()
        {
            _control.Setup(x => x.Shutdown()).Verifiable();
            _peer.Run();
            _control.Verify();
        }

        private Message ReadFinalMessage()
        {
            _peer.Disconnect();
            throw new IOException("done");
        }

        private class Capture<T>
        {
            public T Value { get; set; }
        }
    }
}