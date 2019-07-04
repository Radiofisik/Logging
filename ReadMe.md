---
title: Логирование и перехват
description: Логирование необходимо для отладки, но если в приложении логировать все действия то лог засоряется строчками типа `_logger.LogDebug("something happended {data}", data);` В результате чего код становится трудно читать...
---

## AOP

Логирование необходимо для отладки, но если в приложении логировать все действия то лог засоряется строчками типа `_logger.LogDebug("something happended {data}", data);` В результате чего код становится трудно читать. Возникает желание вынести логирование из основного кода. Концепция выноса подобного кода не нова и существует во многих языках, так в популярном в Java мире фреймворке Spring существует концепция аспектно ориентированного программирования. 

Реализуем этот концепт в мире .Net. Для этого будем использовать интерсепторы https://autofaccn.readthedocs.io/en/latest/advanced/interceptors.html Установим пакеты

```bash
dotnet add package Autofac
dotnet add package Autofac.Extensions.DependencyInjection
dotnet add package Serilog
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Settings.Configuration
dotnet add package Autofac.Extras.DynamicProxy
dotnet add package Castle.Core
```

Создадим сам перехватчик который логирует аргументы вызова

```c#
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Interceptor
{
    public class LoggingInterceptor: IInterceptor
    {
        private readonly ILogger _logger;

        public LoggingInterceptor(ILogger logger)
        {
            _logger = logger;
        }

        public void Intercept(IInvocation invocation)
        {
            using (_logger.BeginScope("{TargetType}.{Method}", invocation.TargetType.Name, invocation.Method.Name))
            {
                    _logger.LogDebug("Arguments: [{Arguments}]", invocation.Arguments.Select(x => JsonConvert.SerializeObject(x)));
                invocation.Proceed();
            }
        }
    }
}

```

Допустим у нас есть сервис с тестовым методом

```c#
 public interface IExampleService: IService
    {
        OutputDto DoSomething(InputDto input);
    }
```

IService - интерфейс - маркер. Зарегистрируем реализацию сервиса и перехватчик

```c#
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
```

Теперь все вызовы методов сервисов IService легируются с аргументами.











> Git репозиторий получившегося проекта https://github.com/Radiofisik/Logging.git