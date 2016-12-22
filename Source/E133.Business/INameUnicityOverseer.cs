using System;
using System.Threading.Tasks;

namespace E133.Business
{
    public interface INameUnicityOverseer
    {
        Task<string> GenerateUniqueName(string name);
    }
}
