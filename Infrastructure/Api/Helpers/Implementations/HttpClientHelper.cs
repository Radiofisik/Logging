using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Infrastructure.Session.Abstraction;
using Newtonsoft.Json;

namespace Infrastructure.Api.Helpers.Implementations
{
    public class HttpClientHelper: IHttpClientHelper
    {
        private readonly Lazy<ISessionStorage> _storage;
        private readonly HttpClient _httpClient;
        private readonly JsonSerializer _serializer;

        public HttpClientHelper(IHttpClientFactory factory, Lazy<ISessionStorage> storage)
        {
            _httpClient = factory.CreateClient(nameof(HttpClientHelper));
            _storage = storage;
            _serializer = new JsonSerializer();
        }

        public Task<TResult> Get<TResult>(string url, params (string key, string value)[] headers)
        {
            return Send<object, TResult>(HttpMethod.Get, url, null, headers);
        }

        public Task<TResult> Post<TInput, TResult>(string url, TInput data, params (string key, string value)[] headers)
        {
            return Send<TInput, TResult>(HttpMethod.Post, url, data, headers);
        }

        public Task<TResult> Put<TInput, TResult>(string url, TInput data, params (string key, string value)[] headers)
        {
            return Send<TInput, TResult>(HttpMethod.Put, url, data, headers);
        }

        public Task<TResult> Delete<TInput, TResult>(string url, TInput data, params (string key, string value)[] headers)
        {
            return Send<TInput, TResult>(HttpMethod.Delete, url, data, headers);
        }

        private async Task<TResult> Send<TInput, TResult>(HttpMethod method, string url, TInput data, params (string key, string value)[] headers)
        {
            var request = new HttpRequestMessage(method, url);
            if (data != null)
            {
                var content = new StringContent(JsonConvert.SerializeObject(data));
                content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");
                request.Content = content;
            }
           
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            foreach (var (key, value) in headers)
            {
                request.Headers.Add(key, value);
            }

            var traceHeaders = _storage.Value.GetTraceHeaders();

            foreach (var sHeader in traceHeaders)
            {
                request.Headers.Add(sHeader.Key, sHeader.Value);
            }

            var response = await _httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();
            using (var streamReader = new StreamReader(await response.Content.ReadAsStreamAsync()))
            {
                return (TResult)_serializer.Deserialize(streamReader, typeof(TResult));
            }
        }
    }
}
