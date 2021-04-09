using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gen9x.Pipeline
{
    internal class Pipeline<TPipeItem, TContext> : IPipeline<TContext>
        where TPipeItem : IPipeItem<TContext>
        where TContext : IPipeContext
    {
        private readonly List<TPipeItem> _pipeline;
        private IEnumerator<TPipeItem> _iterator;

        internal Pipeline()
        {
            _pipeline = new();
        }

        internal void AddItem(TPipeItem item)
        {
            _pipeline.Add(item);
        }

        public async Task ExecuteAsync(TContext context)
        {
            Reset();

            await ExecuteNextAsync(context);
        }

        private async Task ExecuteNextAsync(TContext context)
        {
            if (_iterator.MoveNext())
                await _iterator.Current.ExecuteAsync(context, ExecuteNextAsync);
        }

        public async Task ExecuteAsync(IPipeContext context)
        {
            await ExecuteNextAsync((TContext)context);
        }

        private void Reset()
        {
            _iterator = _pipeline.GetEnumerator();
            _iterator.Reset();
        }
    }
}
