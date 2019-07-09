---
title: Логирование и перехват
description: Логирование необходимо для отладки, но если в приложении логировать все действия то лог засоряется строчками типа `_logger.LogDebug("something happended {data}", data);` В результате чего код становится трудно читать...
---

## AOP

Логирование необходимо для отладки, но если в приложении логировать все действия то лог засоряется строчками типа `_logger.LogDebug("something happended {data}", data);` В результате чего код становится трудно читать. Возникает желание вынести логирование из основного кода. Концепция выноса подобного кода не нова и существует во многих языках, так в популярном в Java мире фреймворке Spring существует концепция аспектно ориентированного программирования. 

Реализуем этот концепт в мире .Net. Для этого будем использовать интерцепторы https://autofaccn.readthedocs.io/en/latest/advanced/interceptors.html Установим пакеты

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

Теперь все вызовы методов сервисов IService легируются с аргументами. Однако остается проблема с асинхронными методами. Они возвращают Task который выполняется уже вне контекста интерцептора.

## Перехват асинхронных вызовов

Допустим мы хотим перехватить асинхронный вызов обернуть его в try/catch и залогировать возникшее исключение в автоматическом режиме. Погуглив можно наткнутся на несколько решений данной проблемы.  Одно из них создать перехватчик с двумя методами, один обрабатывает синхронные вызовы, другой асинхронные.

```c#
namespace Interceptor
{
    public abstract class AsyncInterceptor: IInterceptor
    {
        protected abstract void InterceptSync(IInvocation invocation);

        protected abstract Task InterceptAsync(IInvocation invocation, Type methodReturnType);

        void IInterceptor.Intercept(IInvocation invocation)
        {
            if (!typeof(Task).IsAssignableFrom(invocation.Method.ReturnType))
            {
                InterceptSync(invocation);
                return;
            }
            try
            {
                var method = invocation.Method;

                if ((method != null) && typeof(Task).IsAssignableFrom(method.ReturnType))
                {
                    Task.Factory.StartNew(
                        async () => { await InterceptAsync(invocation, method.ReturnType).ConfigureAwait(true); }
                        , CancellationToken.None).Wait();
                }
            }
            catch (Exception ex)
            {
                //this is not really burring the exception
                //excepiton is going back in the invocation.ReturnValue which 
                //is a Task that failed. with the same excpetion 
                //as ex.
            }
        }
    }
}

```

Сам метод асинхронного перехвата имеет примерно следующий вид

```c#
protected override async Task InterceptAsync(IInvocation invocation, Type methodReturnType)
        {
            using (_logger.BeginScope("{TargetType}.{Method}", invocation.TargetType.Name, invocation.Method.Name))
            {
                try
                {
                    invocation.Proceed();
                    Task result = (Task) invocation.ReturnValue;
                    await result;
                }
                catch (Exception e)
                {
                   //log exception here and modify return to fix it
                }
            }
        }
```

В репозитории к статье ошибка перехватывается и возвращается в виде специального объекта ошибки, который в дальнейшем может быть обработан вышележащими слоями.

```c#
  Type[] typeParams = new Type[] {invocation.Method.ReturnType.GenericTypeArguments[0].GenericTypeArguments[0]};
                    Type constructedType = typeof(Fail<>).MakeGenericType(typeParams);
                    var errorInstance = Activator.CreateInstance(constructedType, e);

                    var returnResult = Activator.CreateInstance(invocation.Method.ReturnType, BindingFlags.Instance
                                                                                              | BindingFlags.NonPublic
                                                                                              | BindingFlags.CreateInstance,
                        null, new object[] {errorInstance}, null, null);
                    invocation.ReturnValue = returnResult;
```



## Serilog Elastic Kibana

Для начала развернем Elastic с Kibana в докере.

```yml
version: '3.5'

services:       
  elastic:
      image: docker.elastic.co/elasticsearch/elasticsearch:6.4.0
      restart: always
      hostname: elastic
      container_name: elastic
      environment:
        - ES_JAVA_OPTS=-Xms4g -Xmx4g
        - cluster.name=elasticl
      ports:
        - 19202:9200
        - 19302:9300
      volumes:
        - elastic_data_l:/usr/share/elasticsearch/data
        - elastic_log_l:/usr/share/elasticsearch/logs

  kibana:
      image: docker.elastic.co/kibana/kibana:6.4.0
      restart: always
      hostname: kibana
      container_name: kibana
      ports:
      - 5602:5601
      environment:
        ELASTICSEARCH_URL: http://elastic:9200
      depends_on:
        - elastic

volumes:
  elastic_data_l: {}
  elastic_log_l: {}
```

запустив `docker-compose up -d` получаем запущенный ES и Kibana по http://docker:5602

Подключаем Serilog к ElasticSearch

```bash
dotnet add package serilog.sinks.elasticsearch
```

```c#
 public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration
                    .ReadFrom.Configuration(hostingContext.Configuration)
                    .Enrich.FromLogContext()
//                    .WriteTo.Console(new CompactJsonFormatter())
                    .WriteTo.Console()
                    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://docker:19202"))
                    {
                        AutoRegisterTemplate = true,
                        AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv6
                    })
                );
```

## Логирование при обработке событий RabbitMQ

Добавим возможность обработки событий https://radiofisik.ru/2019/06/12/rabbit/ . По умолчанию сообщение в Rebus проходит следующие шаги (можно получить добавив в конфигурацию `o.LogPipeline();`)

```
------------------------------------------------------------------------------
Message pipelines
------------------------------------------------------------------------------
Send pipeline:
    Rebus.Pipeline.Send.AssignDefaultHeadersStep
    Rebus.Pipeline.Send.FlowCorrelationIdStep
    Rebus.Pipeline.Send.AutoHeadersOutgoingStep
    Rebus.Pipeline.Send.SerializeOutgoingMessageStep
    Rebus.Pipeline.Send.ValidateOutgoingMessageStep
    Rebus.Pipeline.Send.SendOutgoingMessageStep

Receive pipeline:
    Rebus.Retry.Simple.SimpleRetryStrategyStep
    Rebus.Retry.FailFast.FailFastStep
    Rebus.Pipeline.Receive.HandleDeferredMessagesStep
    Rebus.Pipeline.Receive.DeserializeIncomingMessageStep
    Rebus.Pipeline.Receive.HandleRoutingSlipsStep
    Rebus.Pipeline.Receive.ActivateHandlersStep
    Rebus.Sagas.LoadSagaDataStep
    Rebus.Pipeline.Receive.DispatchIncomingMessageStep
------------------------------------------------------------------------------
```

возможно добавить собственный шаг

```c#
  o.Decorate<IPipeline>(ctx =>
                    {
                        var step = new LoggerStep();
                        var pipeline = ctx.Get<IPipeline>();
                        return new PipelineStepInjector(pipeline).OnReceive(step, PipelineRelativePosition.After, typeof(ActivateHandlersStep));

                    });
```

Сам шаг, логирующий все обработки событий, выглядит примерно так

```c#
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rebus.Logging;
using Rebus.Pipeline;
using Rebus.Transport;

namespace Infrastructure.Steps
{
    public class LoggerStep: IIncomingStep
    {
        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var transactionScope = context.Load<ITransactionContext>();
            var scope = transactionScope.GetOrNull<ILifetimeScope>("current-autofac-lifetime-scope");
            var logger = scope.Resolve<ILogger<LoggerStep>>();

            MessageContext.Current.Headers.TryGetValue("rbs2-sender-address", out string eventSender);
            MessageContext.Current.Headers.TryGetValue("rbs2-msg-type", out string eventType);
            using (logger.BeginScope(new Dictionary<string, object>(){{"exampleParam", "exampleParamValue"}}))
            {
                logger.LogInformation("Event type {EventType} from {EventSender} headers: {Headers}", eventType, eventSender, JsonConvert.SerializeObject(MessageContext.Current.Headers));
                logger.LogDebug("Event body: {Body}", JsonConvert.SerializeObject(MessageContext.Current.Message.Body));
                await next();
            }
        }
    }
}
```

## Логирование запросов API

.Net Core использует паттерн MiddleWare для обработки запросов. Возможно создание собственного MiddleWare для перехвата всех запросов к нижележащим уровням. Как реализовать такое описано [тут]( https://exceptionnotfound.net/using-middleware-to-log-requests-and-responses-in-asp-net-core/). Чуть изменённый код

```c#
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Infrastructure.MiddleWare
{
    public sealed class LoggingMiddleWare
    {
        private readonly RequestDelegate _next;

        public LoggingMiddleWare(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, ILogger logger)
        {
            var body = await GetRequestBodyString(context.Request);

            //Copy a pointer to the original response body stream
            var originalBodyStream = context.Response.Body;

            //Create a new memory stream...
            using (var responseBody = new MemoryStream())
            {
                //...and use that for the temporary response body
                context.Response.Body = responseBody;

                var headers = JsonConvert.SerializeObject(context.Request.Headers.ToDictionary(header => header.Key, header => header.Value));
                //Continue down the Middleware pipeline, eventually returning to this class
                using (logger.BeginScope(new Dictionary<string, object>() { { "exampleParam", "exampleParamValue" } }))
                {
                    using (logger.BeginScope(new Dictionary<string, object> { { "Headers", headers }, { "Body", body } }))
                    {
                        logger.LogInformation($"HTTP request: {context.Request.Scheme} {context.Request.Host}" + "{RequestPath} {QueryString}", context.Request.Path, context.Request.QueryString);
                    }

                    await _next(context);

                    //Format the response from the server
                    var response = await GetResponseBodyString(context.Response);

                    logger.LogDebug("HTTP response status: {status} {body}", context.Response.StatusCode, response);
                }

                //Copy the contents of the new memory stream (which contains the response) to the original stream, which is then returned to the client.
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }

        private async Task<string> GetRequestBodyString(HttpRequest request)
        {
            if (request.ContentType != "application/json")
                return string.Empty;

            var body = request.Body;

            //This line allows us to set the reader for the request back at the beginning of its stream.
            request.EnableRewind();

            //We now need to read the request stream.  First, we create a new byte[] with the same length as the request stream...
            var buffer = new byte[Convert.ToInt32(request.ContentLength)];

            //...Then we copy the entire request stream into the new buffer.
            await request.Body.ReadAsync(buffer, 0, buffer.Length);

            //We convert the byte[] into a string using UTF8 encoding...
            var bodyAsText = Encoding.UTF8.GetString(buffer);

            //..and finally, assign the read body back to the request body, which is allowed because of EnableRewind()
            request.Body = body;

            return bodyAsText;
        }

        private async Task<string> GetResponseBodyString(HttpResponse response)
        {
            if (response.ContentType == "application/json")
                return string.Empty;

            //We need to read the response stream from the beginning...
            response.Body.Seek(0, SeekOrigin.Begin);

            //...and copy it into a string
            string text = await new StreamReader(response.Body).ReadToEndAsync();

            //We need to reset the reader for the response so that the client can read it.
            response.Body.Seek(0, SeekOrigin.Begin);

            return text;
        }
    }
}

```

зарегистрируем MiddleWare `  app.UseMiddleware<LoggingMiddleWare>();`

## Correlation Context

На данный момент логируются в автоматическом режиме:

- Запросы и ответы API
- Вызовы методов сервисов и их ошибки
- Вызовы обработчиков событий

Типичные сценарии логирования которые надо предусмотреть

- Звонит техподдержка и говорит что у пользователя с логином login что-то пошло не так и надо посмотреть что именно. Соответственно необходимо иметь возможность найти сессии пользователя по логину пользователя и привязать все события, запросы и вызовы сервисов к действиям этого пользователя чтобы можно было отследить действия пользователя в общем потоке событий.
- Некоторый баг приводит к тому что у всех пользователей что-то идет не так и возникает исключение, необходимо понять что именно привело к этому исключению, какие действия пользователя...

Во всех этих случаях  необходимо как-то поддерживать единую цепочку событий, для этого введем CorrelationId. Идея не нова и описана во многих блогах например https://www.stevejgordon.co.uk/asp-net-core-correlation-ids В качестве первоначальной затравки многие используют TraceIdentifier. Так данный идентификатор присваивается первому запросу, хранится в некотором классе с временем жизни равным времени жизни запроса. и включается в хедеры всех исходящих вызовов событий RabbitMQ и API. При начале обработки события или запроса проверяется пришел ли в заголовке данный идентификатор и если пришел то используем его в логах и всех дальнейших действиях иначе считаем что запрос первый и используем TraceIdentifier.

Для решения вопроса о трекинге всех действий пользователя можно добавлять в заголовок логин пользователя в качестве которого может быть использован email. Далее данный email должен содержаться во всех записях логов которые были записаны как результат действия пользователя. Остается вопрос что делать с незарегистрированными пользователями.

Так CorrelationId совместно с email в каждом событии покрывают оба обозначенных кейса. Возможно также стоит добавить API с которого пришел запрос. Для реализации необходимо:

- Сгенерировать CorrelationId при обращении пользователя либо взять входящий запомнить в некотором классе - хранилище и использовать
  - в каждом запросе к API
  - в каждом событии в очередь RabbitMQ
  - при каждой записи в лог

Создадим хранилище для контекста с интерфейсом (реализацию см. в репозитории):

```c#
using System.Collections.Generic;

namespace Infrastructure.Session.Abstraction
{
    public interface ISessionStorage
    {
        void SetHeaders(params (string Key, IEnumerable<string> Value)[] headers);

        //adds to every log message
        Dictionary<string, string> GetLoggingHeaders();

        //used as context to call api or enquue messages
        Dictionary<string, string> GetTraceHeaders();
    }
}

```

И MiddleWare которая устанавливает хедеры из запроса

```c#
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Infrastructure.Session.Abstraction;
using Infrastructure.Session.Implementation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Infrastructure.MiddleWare
{
    public class SessionMiddleWare
    {
        private readonly RequestDelegate _next;

        public SessionMiddleWare(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, ISessionStorage sessionStorage, ILogger logger)
        {
            if (!context.Request.Headers.ContainsKey(Headers.Const.RequestId))
            {
                context.Request.Headers.Add(Headers.Const.RequestId, context.TraceIdentifier);
            }

            var headers = context.Request.Headers.Select(x => (x.Key, x.Value.AsEnumerable())).ToArray();
            sessionStorage.SetHeaders(headers);

            await _next(context);
        }
    }
}
```

Зарегистрируем 

```c#
 app.UseMiddleware<SessionMiddleWare>();
 app.UseMiddleware<LoggingMiddleWare>();
```

```c#
  builder.RegisterType<SessionStorage>().AsImplementedInterfaces();
```

В логировании заменим `new Dictionary<string, object>() { { "exampleParam", "exampleParamValue" } }` на полученный из метода GetLoggingHeaders



> Git репозиторий получившегося проекта https://github.com/Radiofisik/Logging.git

Пока не решенные проблемы

- действия незарегистрированных пользователей
- действия одного пользователя с различных устройств
- один запрос может быть залогирован много раз в разных местах, что может затруднить анализ логов, необходимо иметь идентификатор запроса который уникален для этого запроса.