using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Infrastructure.Session.Abstraction;
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

        public async Task Invoke(HttpContext context, ILogger logger, ISessionStorage sessionStorage)
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
                using (logger.BeginScope(sessionStorage.GetLoggingHeaders()))
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
