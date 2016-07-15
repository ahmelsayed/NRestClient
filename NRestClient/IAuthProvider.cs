using System;
using System.Threading.Tasks;

namespace NRestClient
{
    public interface IAuthProvider
    {
        Task<string> GetAuthorizationHeader(Uri uri);
    }
}