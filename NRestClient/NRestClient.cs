using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NRestClient
{
    public class NRestClient : INRestClient
    {
        private readonly IAuthProvider _authProvider;
        private readonly IOptions _options;

        public NRestClient(IAuthProvider authProvider, IOptions options)
        {
            _authProvider = authProvider;
            _options = options;
        }
    }
}
