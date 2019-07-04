using System;
using System.Linq;
using Autofac;
using Autofac.Extras.DynamicProxy;
using Interceptor;
using Services.Abstractions;

namespace Services
{
    public class ServiceRegistrationModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<LoggingInterceptor>();

            var types =
                GetType().Assembly.GetTypes()
                    .Where(type => typeof(IService).IsAssignableFrom(type))
                    .ToArray();

            builder.RegisterTypes(types)
                .InterceptedBy(typeof(LoggingInterceptor))
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope()
                .EnableInterfaceInterceptors();
        }
    }
}
