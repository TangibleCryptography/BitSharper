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
using System.Net;
using System.Threading;
using BitSharper.Store;

namespace BitSharper.Demo
{
    /// <summary>
    /// Downloads the block given a block hash from the localhost node and prints it out.
    /// </summary>
    public static class FetchBlock
    {
        public static void Run(string[] args)
        {
            Console.WriteLine("Connecting to node");
            var @params = NetworkParameters.ProdNet();

            using (var blockStore = new MemoryBlockStore(@params))
            {
                var chain = new BlockChain(@params, blockStore);
                var peer = new Peer(@params, new PeerAddress(IPAddress.Loopback), chain);
                peer.Connect();
                new Thread(peer.Run).Start();

                var blockHash = new Sha256Hash(args[0]);
                var future = peer.BeginGetBlock(blockHash, null, null);
                Console.WriteLine("Waiting for node to send us the requested block: " + blockHash);
                var block = peer.EndGetBlock(future);
                Console.WriteLine(block);
                peer.Disconnect();
            }
        }
    }
}