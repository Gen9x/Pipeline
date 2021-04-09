using System.Threading.Tasks;

namespace Gen9x.Pipeline
{
    public interface IPipeline
    {
        Task ExecuteAsync(IPipeContext context);
    }

    public interface IPipeline<TContext> : IPipeline
        where TContext : IPipeContext
    {
        Task ExecuteAsync(TContext context);

        //void Reset();
    }
}
