//using Microsoft.Extensions.DependencyInjection;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Gen9x.Pipeline
{
    public interface IPipelineBuilder
    {
        void AddItem(Type pipeType);

        IPipeline Build();
    }

    public static class PipelineBuilder
    {
        public static IPipelineBuilder CreatePipeline()
        {
            return new PipelineBuilderImpl();
        }

        public static IPipelineBuilder AddItem<TPipeItem>(this IPipelineBuilder builder)
            where TPipeItem : IPipeItem
        {
            builder.AddItem(typeof(TPipeItem));
            return builder;
        }
    }

    internal class PipelineBuilderImpl : IPipelineBuilder
    {
        private readonly List<Type> _pipeTypes;

        internal PipelineBuilderImpl()
        {
            _pipeTypes = new();
        }

        public void AddItem(Type pipeType) => _pipeTypes.Add(pipeType);

        public IPipeline Build()
        {
            if (_pipeTypes.Count == 0)
                throw new InvalidOperationException("Pipeline is empty");

            List<object> pipeItems = new();

            foreach (var pipeType in _pipeTypes)
            {
                var ctors = pipeType.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
                if (ctors.Length > 0)
                {
                    var ctor = ctors[0];
                    var paramsInfo = ctor.GetParameters();
                    var args = new List<object>(paramsInfo.Length);
                    if (paramsInfo.Length > 0)
                    {
                        foreach (var paramInfo in paramsInfo)
                        {
                            args.Add(Activator.CreateInstance(paramInfo.ParameterType));
                        }
                    }

                    var instance = ctor.Invoke(args.ToArray());
                    pipeItems.Add(instance);
                }
            }

            var pipelineType = typeof(Pipeline<,>).MakeGenericType(new Type[] { typeof(IPipeItem), typeof(IPipeContext) });
            var pipeline = Activator.CreateInstance(pipelineType);
            var addItem = pipelineType.GetMethod("AddItem");

            //addItem.Invoke(pipeline, new object[] { instance });

            return (IPipeline)pipeline;
        }

        public IPipeline Build(IServiceProvider serviceProvider)
        {
            object[] pipeItems = CreatePipeItems(type => serviceProvider.GetService(type) ?? Activator.CreateInstance(type));

            var contextType = _pipeTypes[0]
                .GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipeItem<>))
                .Select(i => i.GetGenericArguments()[0])
                .First();

            var itemType = typeof(IPipeItem<>).MakeGenericType(contextType);
            var pipelineType = typeof(Pipeline<,>).MakeGenericType(itemType, contextType );
            var pipeline = Activator.CreateInstance(pipelineType, true);
            var addItem = pipelineType.GetMethod("AddItem", BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (var item in pipeItems)
            {
                addItem.Invoke(pipeline, new object[] { item });
            }

            return (IPipeline)pipeline;
        }

        private object[] CreatePipeItems(Func<Type, object> createItem)
        {
            if (_pipeTypes.Count == 0)
                throw new InvalidOperationException("Pipeline is empty");

            List<object> pipeItems = new();

            foreach (var pipeType in _pipeTypes)
            {
                var ctors = pipeType.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
                if (ctors.Length > 0)
                {
                    var ctor = ctors[0];
                    var paramsInfo = ctor.GetParameters();
                    var args = new List<object>(paramsInfo.Length);
                    if (paramsInfo.Length > 0)
                    {
                        foreach (var paramInfo in paramsInfo)
                        {
                            var paramValue = createItem(paramInfo.ParameterType);
                            args.Add(paramValue);
                        }
                    }

                    var instance = ctor.Invoke(args.ToArray());
                    pipeItems.Add(instance);
                }
            }

            return pipeItems.ToArray();
        }
    }

    public static class PipelineBuilderDIExtension
    {
        public static IPipeline Build(this IPipelineBuilder builder, IServiceProvider serviceProvider)
        {
            return ((PipelineBuilderImpl)builder).Build(serviceProvider);
        }
    }
}
