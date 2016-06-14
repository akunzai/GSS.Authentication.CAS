using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace GSS.Authentication.CAS.AspNetCore
{
    public class BaseCasContext : BaseContext
    {
        public BaseCasContext(HttpContext context, CasAuthenticationOptions options)
            : base(context)
        {
            Options = options;
        }

        public CasAuthenticationOptions Options { get; }
    }
}
