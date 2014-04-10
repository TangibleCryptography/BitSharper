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
using System.Diagnostics;
using System.IO;
using System.Net;
using BitSharper.Store;

namespace BitSharper.Demo
{
    /// <summary>
    /// PingService demonstrates basic usage of the library. It sits on the network and when it receives coins, simply
    /// sends them right back to the previous owner, determined rather arbitrarily by the address of the first input.
    /// </summary>
    /// <remarks>
    /// If running on TestNet (slow but better than using real coins on ProdNet) do the following:
    /// <ol>
    ///   <li>Backup your current wallet.dat in case of unforeseen problems</li>
    ///   <li>Start your bitcoin client in test mode <code>bitcoin -testnet</code>. This will create a new sub-directory called testnet and should not interfere with normal wallets or operations.</li>
    ///   <li>(Optional) Choose a fresh address</li>
    ///   <li>(Optional) Visit the TestNet faucet (https://testnet.freebitcoins.appspot.com/) to load your client with test coins</li>
    ///   <li>Run <code>BitSharper.Examples PingService -testnet</code></li>
    ///   <li>Wait for the block chain to download</li>
    ///   <li>Send some coins from your bitcoin client to the address provided in the PingService console</li>
    ///   <li>Leave it running until you get the coins back again</li>
    /// </ol><p/>
    /// The testnet can be slow or flaky as it's a shared resource. You can use the <a href="http://sourceforge.net/projects/bitcoin/files/Bitcoin/testnet-in-a-box/">testnet in a box</a>
    /// to do everything purely locally.
    /// </remarks>
    public static class PingService
    {
        public static void Run(string[] args)
        {
            var testNet = args.Length > 0 && string.Equals(args[0], "testnet", StringComparison.InvariantCultureIgnoreCase);
            var @params = testNet ? NetworkParameters.TestNet() : NetworkParameters.ProdNet();
            var filePrefix = testNet ? "pingservice-testnet" : "pingservice-prodnet";

            // Try to read the wallet from storage, create a new one if not possible.
            Wallet wallet;
            var walletFile = new FileInfo(filePrefix + ".wallet");
            try
            {
                wallet = Wallet.LoadFromFile(walletFile);
            }
            catch (IOException)
            {
                wallet = new Wallet(@params);
                wallet.Keychain.Add(new EcKey());
                wallet.SaveToFile(walletFile);
            }
            // Fetch the first key in the wallet (should be the only key).
            var key = wallet.Keychain[0];

            Console.WriteLine(wallet);

            // Load the block chain, if there is one stored locally.
            Console.WriteLine("Reading block store from disk");
            using (var blockStore = new BoundedOverheadBlockStore(@params, new FileInfo(filePrefix + ".blockchain")))
            {
                // Connect to the localhost node. One minute timeout since we won't try any other peers
                Console.WriteLine("Connecting ...");
                var chain = new BlockChain(@params, wallet, blockStore);

                var peerGroup = new PeerGroup(blockStore, @params, chain);
                peerGroup.AddAddress(new PeerAddress(IPAddress.Loopback));
                peerGroup.Start();

                // We want to know when the balance changes.
                wallet.CoinsReceived +=
                    (sender, e) =>
                    {
                        // Running on a peer thread.
                        Debug.Assert(!e.NewBalance.Equals(0));
                        // It's impossible to pick one specific identity that you receive coins from in BitCoin as there
                        // could be inputs from many addresses. So instead we just pick the first and assume they were all
                        // owned by the same person.
                        var input = e.Tx.Inputs[0];
                        var from = input.FromAddress;
                        var value = e.Tx.GetValueSentToMe(wallet);
                        Console.WriteLine("Received " + Utils.BitcoinValueToFriendlyString(value) + " from " + from);
                        // Now send the coins back!
                        var sendTx = wallet.SendCoins(peerGroup, from, value);
                        Debug.Assert(sendTx != null); // We should never try to send more coins than we have!
                        Console.WriteLine("Sent coins back! Transaction hash is " + sendTx.HashAsString);
                        wallet.SaveToFile(walletFile);
                    };

                peerGroup.DownloadBlockChain();
                Console.WriteLine("Send coins to: " + key.ToAddress(@params));
                Console.WriteLine("Waiting for coins to arrive. Press Ctrl-C to quit.");
                // The PeerGroup thread keeps us alive until something kills the process.
            }
        }
    }
}