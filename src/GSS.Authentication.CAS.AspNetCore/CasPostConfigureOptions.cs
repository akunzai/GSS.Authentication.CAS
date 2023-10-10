using System;
using System.Net.Http;
using GSS.Authentication.CAS.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace GSS.Authentication.CAS.AspNetCore;

internal class CasPostConfigureOptions : IPostConfigureOptions<CasAuthenticationOptions>
{
    private readonly IDataProtectionProvider _dataProtection;

    public CasPostConfigureOptions(IDataProtectionProvider dataProtection)
    {
        _dataProtection = dataProtection;
    }

    public void PostConfigure(string? name, CasAuthenticationOptions options)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(name);
#else
        if (name == null) throw new ArgumentNullException(nameof(name));
#endif
        options.DataProtectionProvider ??= _dataProtection;
        
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (options.StateDataFormat == null)
        {
            var dataProtector = options.DataProtectionProvider.CreateProtector(
                typeof(CasAuthenticationHandler).FullName!, name, "v1");
            options.StateDataFormat = new PropertiesDataFormat(dataProtector);
        }

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (options.Backchannel == null)
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
#pragma warning disable IDE0067 // Dispose objects before losing scope
            options.Backchannel = new HttpClient(options.BackchannelHttpHandler ?? new HttpClientHandler());
#pragma warning restore IDE0067 // Dispose objects before losing scope
#pragma warning restore CA2000 // Dispose objects before losing scope
            options.Backchannel.DefaultRequestHeaders.UserAgent.ParseAdd("ASP.NET Core CAS handler");
            options.Backchannel.Timeout = options.BackchannelTimeout;
            options.Backchannel.MaxResponseContentBufferSize = 1024 * 1024 * 10; // 10 MB
        }
        
        options.ServiceTicketValidator ??= new Cas30ServiceTicketValidator(options, options.Backchannel);
    }
}