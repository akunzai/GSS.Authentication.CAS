using System;
using System.Net.Http;
using GSS.Authentication.CAS.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace GSS.Authentication.CAS.AspNetCore;

internal class CasPostConfigureOptions : IPostConfigureOptions<CasAuthenticationOptions>
{
    private readonly IDataProtectionProvider _dataProtection;
    private readonly IConfiguration _configuration;

    public CasPostConfigureOptions(IDataProtectionProvider dataProtection, IConfiguration configuration)
    {
        _dataProtection = dataProtection;
        _configuration = configuration;
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
            options.Backchannel = new HttpClient(options.BackchannelHttpHandler ?? new HttpClientHandler());
            options.Backchannel.DefaultRequestHeaders.UserAgent.ParseAdd("ASP.NET Core CAS handler");
            options.Backchannel.Timeout = options.BackchannelTimeout;
            options.Backchannel.MaxResponseContentBufferSize = 1024 * 1024 * 10; // 10 MB
        }

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (options.ServiceTicketValidator == null)
        {
            var protocolVersion = _configuration.GetValue("CAS:ProtocolVersion", 3);
            options.ServiceTicketValidator = protocolVersion switch
            {
                1 => new Cas10ServiceTicketValidator(options, options.Backchannel),
                2 => new Cas20ServiceTicketValidator(options, options.Backchannel),
                _ => new Cas30ServiceTicketValidator(options, options.Backchannel)
            };
        }
    }
}