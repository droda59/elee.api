using System;
using System.Threading.Tasks;

namespace E133.Business
{
    public interface IHtmlLoader : IDisposable
    {
        Task<string> ReadHtmlAsync(Uri uri);

        void Initialize();
    }
}
