using System;
using System.Threading.Tasks;

namespace Gen9x.Pipeline
{
    public interface IPipeItem
    {
    }

    public interface IPipeItem<TContext> : IPipeItem
        where TContext : IPipeContext
    {
        Task ExecuteAsync(TContext context, Func<TContext, Task> next);
    }
}
