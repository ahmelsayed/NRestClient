using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NRestClient
{
    public static class Constants
    {
        public static string UserAgent => $"NRestClient/{typeof(Constants).Assembly.GetName().Version}";
        public const string JsonContentType = "application/json";
    }
}
