using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FuncLite
{
    public static class JsonExtensions
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
    }
}
