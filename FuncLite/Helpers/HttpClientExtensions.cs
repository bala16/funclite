using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace FuncLite
{
    public static class HttpClientExtensions
    {
        // Replicate some helpers since Core doesn't seem to support them

        public static async Task<T> ReadAsAsync<T>(this HttpContent content)
        {
            string json = await content.ReadAsStringAsync();
            return (dynamic)JObject.Parse(json);
        }

        public static Task<HttpResponseMessage> PutAsJsonAsync<T>(this HttpClient client, string requestUri, T value)
        {
            var httpContent = new StringContent(JsonConvert.SerializeObject(value), Encoding.UTF8, "application/json");
            return client.PutAsync(requestUri, httpContent);
        }

        public static async Task<HttpResponseMessage> PutZipFile(this HttpClient client, string uri, string localZipPath)
        {
            using (var stream = File.OpenRead(localZipPath))
            {
                return await PutZipStream(client, uri, stream);
            }
        }

        public static async Task<HttpResponseMessage> PutZipStream(this HttpClient client, string uri, Stream zipFile)
        {
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Put;
                request.RequestUri = new Uri(uri);
                request.Headers.IfMatch.Add(EntityTagHeaderValue.Any);
                request.Content = new StreamContent(zipFile);
                return await client.SendAsync(request);
            }
        }
    }
}
