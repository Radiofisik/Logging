using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Controllers;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Handlers;
using Infrastructure.Api.Helpers.Implementations;
using Infrastructure.MiddleWare;
using Infrastructure.Session.Implementation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Services;
using Swashbuckle.AspNetCore.Swagger;

namespace AutoFacLogging
{
    public class Startup
    {
        private IContainer _container;

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "My API", Version = "v1" });
            });

            services.AddMvcCore()
                .AddJsonFormatters()
                .AddJsonOptions(options => options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc)
                .AddApiExplorer() // to Swagger UI
                .AddApplicationPart(typeof(TestController).Assembly)
                .AddCors();

            services.AddHttpClient();

            var builder = new ContainerBuilder();
            builder.Populate(services);
            builder.RegisterType<SessionStorage>().InstancePerLifetimeScope().AsImplementedInterfaces();
            builder.RegisterType<HttpClientHelper>().AsImplementedInterfaces();

            builder.RegisterSource(new CustomLoggerRegistrator());
            builder.RegisterModule<ServiceRegistrationModule>();
            builder.RegisterModule<HandlerRegistrationModule>();
            builder.RegisterType<OnStart>().AsImplementedInterfaces();
            _container = builder.Build();
            return new AutofacServiceProvider(this._container);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(
                builder => builder.AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowAnyOrigin()
                    .AllowCredentials()
            );

            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), 
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });

            app.UseMiddleware<SessionMiddleWare>();
            app.UseMiddleware<LoggingMiddleWare>();

            app.UseMvc();
        }
    }
}
