using System;
using System.Collections.Generic;
using Moq.Language.Flow;

namespace BitSharper.Test.Moq
{
    public static class SetupExtensions
    {
        public static IReturnsResult<TMock> Returns<TMock, TResult>(this ISetup<TMock, TResult> setup, params Func<TResult>[] funcs)
            where TMock : class
        {
            var sequence = new Queue<Func<TResult>>(funcs);
            return setup.Returns(() => sequence.Dequeue()());
        }
    }
}