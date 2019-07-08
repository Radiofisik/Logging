using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autofac;
using Infrastructure.Steps;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;

namespace Handlers
{
    public class HandlerRegistrationModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var types =
                GetType().Assembly.GetTypes()
                    .Where(type => typeof(IHandleMessages).IsAssignableFrom(type))
                    .ToArray();

            builder.RegisterTypes(types)
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope();

            builder.RegisterRebus((configurer, context) => configurer
                .Logging(l => l.Serilog())
                .Transport(t => t.UseRabbitMq("amqp://docker", "testappqueue"))
                .Options(o => {
                    o.Decorate<IPipeline>(ctx =>
                    {
                        var step = new LoggerStep();
                        var pipeline = ctx.Get<IPipeline>();
                        return new PipelineStepInjector(pipeline).OnReceive(step, PipelineRelativePosition.After, typeof(ActivateHandlersStep));

                    });
                    o.LogPipeline(true);
                    o.SetNumberOfWorkers(1);
                    o.SetMaxParallelism(30);
                }));

            builder.RegisterType<EventSubscriber>().AsImplementedInterfaces();

        }
    }
}
