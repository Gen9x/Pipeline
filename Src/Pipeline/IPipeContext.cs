using System.Threading;

namespace Gen9x.Pipeline
{
    public interface IPipeContext
    {
        public CancellationToken CancellationToken { get; }
    }
}
