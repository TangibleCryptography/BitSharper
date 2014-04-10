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
    /// This example shows how to solve the challenge Hal posted here:<p/>
    ///   <a href="http://www.bitcoin.org/smf/index.php?topic=3638.0"/><p/>
    /// in which a private key with some coins associated with it is published. The goal is to import the private key,
    /// claim the coins and then send them to a different address.
    /// </summary>
    public static class PrivateKeys
    {
        public static void Run(string[] args)
        {
            // TODO: Assumes production network not testnet. Make it selectable.
            var @params = NetworkParameters.ProdNet();
            try
            {
                // Decode the private key from Satoshi's Base58 variant. If 51 characters long then it's from BitCoins
                // "dumpprivkey" command and includes a version byte and checksum. Otherwise assume it's a raw key.
                EcKey key;
                if (args[0].Length == 51)
                {
                    var dumpedPrivateKey = new DumpedPrivateKey(@params, args[0]);
                    key = dumpedPrivateKey.Key;
                }
                else
                {
                    var privKey = Base58.DecodeToBigInteger(args[0]);
                    key = new EcKey(privKey);
                }
                Console.WriteLine("Address from private key is: " + key.ToAddress(@params));
                // And the address ...
                var destination = new Address(@params, args[1]);

                // Import the private key to a fresh wallet.
                var wallet = new Wallet(@params);
                wallet.AddKey(key);

                // Find the transactions that involve those coins.
                using (var blockStore = new MemoryBlockStore(@params))
                {
                    var chain = new BlockChain(@params, wallet, blockStore);

                    var peerGroup = new PeerGroup(blockStore, @params, chain);
                    peerGroup.AddAddress(new PeerAddress(IPAddress.Loopback));
                    peerGroup.Start();
                    peerGroup.DownloadBlockChain();
                    peerGroup.Stop();

                    // And take them!
                    Console.WriteLine("Claiming " + Utils.BitcoinValueToFriendlyString(wallet.GetBalance()) + " coins");
                    wallet.SendCoins(peerGroup, destination, wallet.GetBalance());
                    // Wait a few seconds to let the packets flush out to the network (ugly).
                    Thread.Sleep(5000);
                }
            }
            catch (IndexOutOfRangeException)
            {
                Console.WriteLine("First arg should be private key in Base58 format. Second argument should be address to send to.");
            }
        }
    }
}