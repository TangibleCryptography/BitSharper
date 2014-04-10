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

namespace BitSharper.Test
{
    public static class TestUtils
    {
        public static Transaction CreateFakeTx(NetworkParameters @params, ulong nanocoins, Address to)
        {
            var t = new Transaction(@params);
            var o1 = new TransactionOutput(@params, t, nanocoins, to);
            t.AddOutput(o1);
            // Make a previous tx simply to send us sufficient coins. This prev tx is not really valid but it doesn't
            // matter for our purposes.
            var prevTx = new Transaction(@params);
            var prevOut = new TransactionOutput(@params, prevTx, nanocoins, to);
            prevTx.AddOutput(prevOut);
            // Connect it.
            t.AddInput(prevOut);
            return t;
        }

        public class BlockPair
        {
            public StoredBlock StoredBlock { get; set; }
            public Block Block { get; set; }
        }

        // Emulates receiving a valid block that builds on top of the chain.
        public static BlockPair CreateFakeBlock(NetworkParameters @params, IBlockStore blockStore, params Transaction[] transactions)
        {
            var b = MakeTestBlock(@params, blockStore);
            // Coinbase tx was already added.
            foreach (var tx in transactions)
                b.AddTransaction(tx);
            b.Solve();
            var pair = new BlockPair();
            pair.Block = b;
            pair.StoredBlock = blockStore.GetChainHead().Build(b);
            blockStore.Put(pair.StoredBlock);
            blockStore.SetChainHead(pair.StoredBlock);
            return pair;
        }

        /// <exception cref="BlockStoreException"/>
        public static Block MakeTestBlock(NetworkParameters @params, IBlockStore blockStore)
        {
            return blockStore.GetChainHead().Header.CreateNextBlock(new EcKey().ToAddress(@params));
        }

        /// <exception cref="BlockStoreException"/>
        public static Block MakeSolvedTestBlock(NetworkParameters @params, IBlockStore blockStore)
        {
            var b = blockStore.GetChainHead().Header.CreateNextBlock(new EcKey().ToAddress(@params));
            b.Solve();
            return b;
        }

        /// <exception cref="BlockStoreException"/>
        public static Block MakeSolvedTestBlock(NetworkParameters @params, Block prev)
        {
            var b = prev.CreateNextBlock(new EcKey().ToAddress(@params));
            b.Solve();
            return b;
        }
    }
}