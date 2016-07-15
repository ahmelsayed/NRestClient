using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NRestClient
{
    public class NRestClient : INRestClient
    {
        private readonly IAuthProvider _authProvider;
        private readonly INRestClientOptions _options;
        private readonly HttpMessageHandler _httpMessageHandler;
        private readonly Random rand = new Random();

        public NRestClient(IAuthProvider authProvider, INRestClientOptions options)
        {
            _authProvider = authProvider;
            _options = options;
        }

        public NRestClient(HttpMessageHandler httpMessageHandler, INRestClientOptions options)
        {
            _httpMessageHandler = httpMessageHandler;
            _options = options;
        }

        public async Task<T> Lift<T>(HttpMethod httpMethod, Uri uri, object objectPayload = null, Func<dynamic, dynamic> selector = null)
        {
            var response = await HttpInvoke(httpMethod, uri, objectPayload);
            response.EnsureSuccessStatusCode();

            var content = JsonConvert.DeserializeObject<JToken>(await response.Content.ReadAsStringAsync());

            if (content != null)
            {
                if (selector != null)
                {
                    content = selector(content);
                }

                return content.ToObject<T>();
            }
            else
            {
                return default(T);
            }
        }

        public Task<T> Lift<T>(HttpMethod httpMethod, Uri uri, object objectPayload = null, Func<JToken, JToken> selector = null)
        {
            return Lift<T>(httpMethod, uri, objectPayload, (Func<dynamic, dynamic>) selector);
        }

        public Task<HttpResponseMessage> Get(Uri uri)
        {
            return HttpInvoke(HttpMethod.Get, uri);
        }

        public Task<HttpResponseMessage> Post(Uri uri, object objectPayload = null)
        {
            return HttpInvoke(HttpMethod.Post, uri, objectPayload);
        }

        public Task<HttpResponseMessage> Put(Uri uri, object objectPayload = null)
        {
            return HttpInvoke(HttpMethod.Put, uri, objectPayload);
        }

        public Task<HttpResponseMessage> Delete(Uri uri, object objectPayload = null)
        {
            return HttpInvoke(HttpMethod.Delete, uri, objectPayload);
        }

        public Task<HttpResponseMessage> Patch(Uri uri, object objectPayload = null)
        {
            return HttpInvoke(uri, "patch", objectPayload);
        }

        public Task<HttpResponseMessage> HttpInvoke(HttpMethod method, Uri uri, object objectPayload = null)
        {
            return HttpInvoke(method.Method, uri, objectPayload);
        }

        public async Task<HttpResponseMessage> HttpInvoke(string method, Uri uri, object objectPayload = null)
        {
            var socketTrials = 10;
            var retries = _options.RetryCount;
            while (true)
            {
                try
                {
                    var response = await HttpInvoke(uri, method, objectPayload);

                    if (!response.IsSuccessStatusCode &&_options.RetryCount> 0)
                    {
                        while (retries > 0)
                        {
                            response = await HttpInvoke(uri, method, objectPayload);
                            if (response.IsSuccessStatusCode)
                            {
                                return response;
                            }
                            else
                            {
                                retries--;
                            }
                        }
                    }
                    return response;
                }
                catch (SocketException)
                {
                    if (socketTrials <= 0) throw;
                    socketTrials--;
                }
                catch (Exception)
                {
                    if (retries <= 0) throw;
                    retries--;
                }
                await Task.Delay(rand.Next(1000, 10000));
            }
        }

        private async Task<HttpResponseMessage> HttpInvoke(Uri uri, string verb, object objectPayload)
        {
            var payload = JsonConvert.SerializeObject(objectPayload);
            using (var client = new HttpClient(_httpMessageHandler ?? new HttpClientHandler()))
            {
                client.DefaultRequestHeaders.Add("Authorization", await _authProvider.GetAuthorizationHeader(uri));
                client.DefaultRequestHeaders.Add("User-Agent", Constants.UserAgent);
                client.DefaultRequestHeaders.Add("Accept", Constants.JsonContentType);
                client.DefaultRequestHeaders.Add("x-ms-request-id", Guid.NewGuid().ToString());

                HttpResponseMessage response = null;
                if (String.Equals(verb, "get", StringComparison.OrdinalIgnoreCase))
                {
                    response = await client.GetAsync(uri).ConfigureAwait(false);
                }
                else if (String.Equals(verb, "delete", StringComparison.OrdinalIgnoreCase))
                {
                    response = await client.DeleteAsync(uri).ConfigureAwait(false);
                }
                else if (String.Equals(verb, "post", StringComparison.OrdinalIgnoreCase))
                {
                    response = await client.PostAsync(uri, new StringContent(payload ?? String.Empty, Encoding.UTF8, Constants.JsonContentType)).ConfigureAwait(false);
                }
                else if (String.Equals(verb, "put", StringComparison.OrdinalIgnoreCase))
                {
                    response = await client.PutAsync(uri, new StringContent(payload ?? String.Empty, Encoding.UTF8, Constants.JsonContentType)).ConfigureAwait(false);
                }
                else if (String.Equals(verb, "patch", StringComparison.OrdinalIgnoreCase))
                {
                    using (var message = new HttpRequestMessage(new HttpMethod("PATCH"), uri))
                    {
                        message.Content = new StringContent(payload ?? String.Empty, Encoding.UTF8, Constants.JsonContentType);
                        response = await client.SendAsync(message).ConfigureAwait(false);
                    }
                }
                else
                {
                    throw new InvalidOperationException(String.Format("Invalid http verb '{0}'!", verb));
                }

                return response;
            }
        }
    }
}
