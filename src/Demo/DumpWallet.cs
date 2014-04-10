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
using System.IO;

namespace BitSharper.Demo
{
    /// <summary>
    /// DumpWallet loads a serialized wallet and prints information about what it contains.
    /// </summary>
    public static class DumpWallet
    {
        public static void Run(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: java com.google.bitcoin.examples.DumpWallet <filename>");
                return;
            }

            var wallet = Wallet.LoadFromFile(new FileInfo(args[0]));
            Console.WriteLine(wallet.ToString());
        }
    }
}