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

using BitSharper.Store;
using NUnit.Framework;

namespace BitSharper.Test
{
    [TestFixture]
    public class ChainSplitTests
    {
        private NetworkParameters _unitTestParams;
        private Wallet _wallet;
        private BlockChain _chain;
        private IBlockStore _chainBlockStore;
        private Address _coinbaseTo;
        private Address _someOtherGuy;

        [SetUp]
        public void SetUp()
        {
            _unitTestParams = NetworkParameters.UnitTests();
            _wallet = new Wallet(_unitTestParams);
            _wallet.AddKey(new EcKey());
            _chainBlockStore = new MemoryBlockStore(_unitTestParams);
            _chain = new BlockChain(_unitTestParams, _wallet, _chainBlockStore);
            _coinbaseTo = _wallet.Keychain[0].ToAddress(_unitTestParams);
            _someOtherGuy = new EcKey().ToAddress(_unitTestParams);
        }

        [TearDown]
        public void TearDown()
        {
            _chainBlockStore.Dispose();
        }

        [Test]
        public void TestForking1()
        {
            // Check that if the block chain forks, we end up using the right chain. Only tests inbound transactions
            // (receiving coins). Checking that we understand reversed spends is in testForking2.

            // TODO: Change this test to not use coinbase transactions as they are special (maturity rules).
            var reorgHappened = false;
            _wallet.Reorganized += (sender, e) => reorgHappened = true;

            // Start by building a couple of blocks on top of the genesis block.
            var b1 = _unitTestParams.GenesisBlock.CreateNextBlock(_coinbaseTo);
            var b2 = b1.CreateNextBlock(_coinbaseTo);
            Assert.IsTrue(_chain.Add(b1));
            Assert.IsTrue(_chain.Add(b2));
            Assert.IsFalse(reorgHappened);
            // We got two blocks which generated 50 coins each, to us.
            Assert.AreEqual("100.00", Utils.BitcoinValueToFriendlyString(_wallet.GetBalance()));
            // We now have the following chain:
            //     genesis -> b1 -> b2
            //
            // so fork like this:
            //
            //     genesis -> b1 -> b2
            //                  \-> b3
            //
            // Nothing should happen at this point. We saw b2 first so it takes priority.
            var b3 = b1.CreateNextBlock(_someOtherGuy);
            Assert.IsTrue(_chain.Add(b3));
            Assert.IsFalse(reorgHappened); // No re-org took place.
            Assert.AreEqual("100.00", Utils.BitcoinValueToFriendlyString(_wallet.GetBalance()));
            // Now we add another block to make the alternative chain longer.
            Assert.IsTrue(_chain.Add(b3.CreateNextBlock(_someOtherGuy)));
            Assert.IsTrue(reorgHappened); // Re-org took place.
            reorgHappened = false;
            //
            //     genesis -> b1 -> b2
            //                  \-> b3 -> b4
            //
            // We lost some coins! b2 is no longer a part of the best chain so our available balance should drop to 50.
            Assert.AreEqual("50.00", Utils.BitcoinValueToFriendlyString(_wallet.GetBalance()));
            // ... and back to the first chain.
            var b5 = b2.CreateNextBlock(_coinbaseTo);
            var b6 = b5.CreateNextBlock(_coinbaseTo);
            Assert.IsTrue(_chain.Add(b5));
            Assert.IsTrue(_chain.Add(b6));
            //
            //     genesis -> b1 -> b2 -> b5 -> b6
            //                  \-> b3 -> b4
            //
            Assert.IsTrue(reorgHappened);
            Assert.AreEqual("200.00", Utils.BitcoinValueToFriendlyString(_wallet.GetBalance()));
        }

        [Test]
        public void TestForking2()
        {
            // Check that if the chain forks and new coins are received in the alternate chain our balance goes up
            // after the re-org takes place.
            var b1 = _unitTestParams.GenesisBlock.CreateNextBlock(_someOtherGuy);
            var b2 = b1.CreateNextBlock(_someOtherGuy);
            Assert.IsTrue(_chain.Add(b1));
            Assert.IsTrue(_chain.Add(b2));
            //     genesis -> b1 -> b2
            //                  \-> b3 -> b4
            Assert.AreEqual(0UL, _wallet.GetBalance());
            var b3 = b1.CreateNextBlock(_coinbaseTo);
            var b4 = b3.CreateNextBlock(_someOtherGuy);
            Assert.IsTrue(_chain.Add(b3));
            Assert.AreEqual(0UL, _wallet.GetBalance());
            Assert.IsTrue(_chain.Add(b4));
            Assert.AreEqual("50.00", Utils.BitcoinValueToFriendlyString(_wallet.GetBalance()));
        }

        [Test]
        public void TestForking3()
        {
            // Check that we can handle our own spends being rolled back by a fork.
            var b1 = _unitTestParams.GenesisBlock.CreateNextBlock(_coinbaseTo);
            _chain.Add(b1);
            Assert.AreEqual("50.00", Utils.BitcoinValueToFriendlyString(_wallet.GetBalance()));
            var dest = new EcKey().ToAddress(_unitTestParams);
            var spend = _wallet.CreateSend(dest, Utils.ToNanoCoins(10, 0));
            _wallet.ConfirmSend(spend);
            // Waiting for confirmation ...
            Assert.AreEqual(0UL, _wallet.GetBalance());
            var b2 = b1.CreateNextBlock(_someOtherGuy);
            b2.AddTransaction(spend);
            b2.Solve();
            _chain.Add(b2);
            Assert.AreEqual(Utils.ToNanoCoins(40, 0), _wallet.GetBalance());
            // genesis -> b1 (receive coins) -> b2 (spend coins)
            //                               \-> b3 -> b4
            var b3 = b1.CreateNextBlock(_someOtherGuy);
            var b4 = b3.CreateNextBlock(_someOtherGuy);
            _chain.Add(b3);
            _chain.Add(b4);
            // b4 causes a re-org that should make our spend go inactive. Because the inputs are already spent our
            // available balance drops to zero again.
            Assert.AreEqual(0UL, _wallet.GetBalance(Wallet.BalanceType.Available));
            // We estimate that it'll make it back into the block chain (we know we won't double spend).
            // assertEquals(Utils.toNanoCoins(40, 0), wallet.getBalance(Wallet.BalanceType.ESTIMATED));
        }

        [Test]
        public void TestForking4()
        {
            // Check that we can handle external spends on an inactive chain becoming active. An external spend is where
            // we see a transaction that spends our own coins but we did not broadcast it ourselves. This happens when
            // keys are being shared between wallets.
            var b1 = _unitTestParams.GenesisBlock.CreateNextBlock(_coinbaseTo);
            _chain.Add(b1);
            Assert.AreEqual("50.00", Utils.BitcoinValueToFriendlyString(_wallet.GetBalance()));
            var dest = new EcKey().ToAddress(_unitTestParams);
            var spend = _wallet.CreateSend(dest, Utils.ToNanoCoins(50, 0));
            // We do NOT confirm the spend here. That means it's not considered to be pending because createSend is
            // stateless. For our purposes it is as if some other program with our keys created the tx.
            //
            // genesis -> b1 (receive 50) --> b2
            //                            \-> b3 (external spend) -> b4
            var b2 = b1.CreateNextBlock(_someOtherGuy);
            _chain.Add(b2);
            var b3 = b1.CreateNextBlock(_someOtherGuy);
            b3.AddTransaction(spend);
            b3.Solve();
            _chain.Add(b3);
            // The external spend is not active yet.
            Assert.AreEqual(Utils.ToNanoCoins(50, 0), _wallet.GetBalance());
            var b4 = b3.CreateNextBlock(_someOtherGuy);
            _chain.Add(b4);
            // The external spend is now active.
            Assert.AreEqual(Utils.ToNanoCoins(0, 0), _wallet.GetBalance());
        }

        [Test]
        public void TestDoubleSpendOnFork()
        {
            // Check what happens when a re-org happens and one of our confirmed transactions becomes invalidated by a
            // double spend on the new best chain.

            var eventCalled = false;
            _wallet.DeadTransaction += (sender, e) => eventCalled = true;

            var b1 = _unitTestParams.GenesisBlock.CreateNextBlock(_coinbaseTo);
            _chain.Add(b1);

            var t1 = _wallet.CreateSend(_someOtherGuy, Utils.ToNanoCoins(10, 0));
            var yetAnotherGuy = new EcKey().ToAddress(_unitTestParams);
            var t2 = _wallet.CreateSend(yetAnotherGuy, Utils.ToNanoCoins(20, 0));
            _wallet.ConfirmSend(t1);
            // Receive t1 as confirmed by the network.
            var b2 = b1.CreateNextBlock(new EcKey().ToAddress(_unitTestParams));
            b2.AddTransaction(t1);
            b2.Solve();
            _chain.Add(b2);

            // Now we make a double spend become active after a re-org.
            var b3 = b1.CreateNextBlock(new EcKey().ToAddress(_unitTestParams));
            b3.AddTransaction(t2);
            b3.Solve();
            _chain.Add(b3); // Side chain.
            var b4 = b3.CreateNextBlock(new EcKey().ToAddress(_unitTestParams));
            _chain.Add(b4); // New best chain.

            // Should have seen a double spend.
            Assert.IsTrue(eventCalled);
            Assert.AreEqual(Utils.ToNanoCoins(30, 0), _wallet.GetBalance());
        }

        [Test]
        public void TestDoubleSpendOnForkPending()
        {
            // Check what happens when a re-org happens and one of our UNconfirmed transactions becomes invalidated by a
            // double spend on the new best chain.

            Transaction eventDead = null;
            Transaction eventReplacement = null;
            _wallet.DeadTransaction +=
                (sender, e) =>
                {
                    eventDead = e.DeadTx;
                    eventReplacement = e.ReplacementTx;
                };

            // Start with 50 coins.
            var b1 = _unitTestParams.GenesisBlock.CreateNextBlock(_coinbaseTo);
            _chain.Add(b1);

            var t1 = _wallet.CreateSend(_someOtherGuy, Utils.ToNanoCoins(10, 0));
            var yetAnotherGuy = new EcKey().ToAddress(_unitTestParams);
            var t2 = _wallet.CreateSend(yetAnotherGuy, Utils.ToNanoCoins(20, 0));
            _wallet.ConfirmSend(t1);
            // t1 is still pending ...
            var b2 = b1.CreateNextBlock(new EcKey().ToAddress(_unitTestParams));
            _chain.Add(b2);
            Assert.AreEqual(Utils.ToNanoCoins(0, 0), _wallet.GetBalance());
            Assert.AreEqual(Utils.ToNanoCoins(40, 0), _wallet.GetBalance(Wallet.BalanceType.Estimated));

            // Now we make a double spend become active after a re-org.
            // genesis -> b1 -> b2 [t1 pending]
            //              \-> b3 (t2) -> b4
            var b3 = b1.CreateNextBlock(new EcKey().ToAddress(_unitTestParams));
            b3.AddTransaction(t2);
            b3.Solve();
            _chain.Add(b3); // Side chain.
            var b4 = b3.CreateNextBlock(new EcKey().ToAddress(_unitTestParams));
            _chain.Add(b4); // New best chain.

            // Should have seen a double spend against the pending pool.
            Assert.AreEqual(t1, eventDead);
            Assert.AreEqual(t2, eventReplacement);
            Assert.AreEqual(Utils.ToNanoCoins(30, 0), _wallet.GetBalance());

            // ... and back to our own parallel universe.
            var b5 = b2.CreateNextBlock(new EcKey().ToAddress(_unitTestParams));
            _chain.Add(b5);
            var b6 = b5.CreateNextBlock(new EcKey().ToAddress(_unitTestParams));
            _chain.Add(b6);
            // genesis -> b1 -> b2 -> b5 -> b6 [t1 pending]
            //              \-> b3 [t2 inactive] -> b4
            Assert.AreEqual(Utils.ToNanoCoins(0, 0), _wallet.GetBalance());
            Assert.AreEqual(Utils.ToNanoCoins(40, 0), _wallet.GetBalance(Wallet.BalanceType.Estimated));
        }
    }
}