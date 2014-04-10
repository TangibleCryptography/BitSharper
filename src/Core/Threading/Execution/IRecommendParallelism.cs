namespace BitSharper.Threading.Execution
{
    internal interface IRecommendParallelism // NET_ONLY
    {
        int MaxParallelism { get; }
    }
}