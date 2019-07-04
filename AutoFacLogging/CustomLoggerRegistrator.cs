using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using Autofac.Core.Activators.Delegate;
using Autofac.Core.Lifetime;
using Autofac.Core.Registration;
using Microsoft.Extensions.Logging;

namespace AutoFacLogging
{
    public class CustomLoggerRegistrator : IRegistrationSource
    {
        public IEnumerable<IComponentRegistration> RegistrationsFor(Service service, Func<Service, IEnumerable<IComponentRegistration>> registrationAccessor)
        {
            var swt = service as IServiceWithType;
            if (swt == null || !typeof(ILogger).IsAssignableFrom(swt.ServiceType))
            {
                // It's not a request for the base handler type, so skip it.
                return Enumerable.Empty<IComponentRegistration>();
            }
            // This is where the magic happens!
            var registration = new ComponentRegistration(
                Guid.NewGuid(),
                new DelegateActivator(swt.ServiceType, (c, p) =>
                {
                    var loggerFactory = c.Resolve<ILoggerFactory>();

                    Func<ILoggerFactory, ILogger> method = null;
                    if (swt.ServiceType.IsGenericType)
                    {
                        var parameterType = swt.ServiceType.GenericTypeArguments[0];
                        method = factory => (ILogger)Activator.CreateInstance(typeof(Logger<>).MakeGenericType(new[] { parameterType }), factory);
                    }
                    else
                    {
                        method = factory => factory.CreateLogger("Generic Logger");
                    }

                    return method(loggerFactory);
                }),
                new CurrentScopeLifetime(),
                InstanceSharing.None,
                InstanceOwnership.OwnedByLifetimeScope,
                new[] { service },
                new Dictionary<string, object>());

            return new IComponentRegistration[] { registration };
        }

        public bool IsAdapterForIndividualComponents => false;
    }
}
