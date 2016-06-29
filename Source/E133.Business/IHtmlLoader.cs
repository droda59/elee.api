using System;
using System.Threading.Tasks;

namespace E133.Business
{
    public interface IHtmlLoader
    {
        Task<string> ReadHtmlAsync(Uri uri);
    }
}
