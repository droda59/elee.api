using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace E133.Business
{
    internal class HtmlLoader : IHtmlLoader
    {
        private HttpClient _httpClient;

        public void Initialize()
        {
            this._httpClient = new HttpClient();
        }

        public void Dispose()
        {
            this._httpClient.Dispose();
            this._httpClient = null;
        }

        public async Task<string> ReadHtmlAsync(Uri uri)
        {
            string content = null;
            var data = await this._httpClient.GetAsync(uri.AbsoluteUri);
            if (data.IsSuccessStatusCode)
            {
                content = await data.Content.ReadAsStringAsync();
            }
            
            return content;
        }
    }
}