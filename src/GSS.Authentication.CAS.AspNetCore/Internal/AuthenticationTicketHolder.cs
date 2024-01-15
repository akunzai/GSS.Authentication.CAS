using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace GSS.Authentication.CAS.AspNetCore.Internal;

internal struct AuthenticationTicketHolder
{
    public AuthenticationTicketHolder(AuthenticationTicket ticket)
    {
        AuthenticationScheme = ticket.AuthenticationScheme;
        Claims = ticket.Principal.Claims.Select(x => new ClaimHolder(x));
        var identity = ticket.Principal.Identity as ClaimsIdentity;
        AuthenticationType = identity?.AuthenticationType;
        NameClaimType = identity?.NameClaimType;
        RoleClaimType = identity?.RoleClaimType;
        Properties = ticket.Properties.Items;
    }

    public string AuthenticationScheme { get; set; }
    
    public string? AuthenticationType { get; set; }

    public IEnumerable<ClaimHolder> Claims { get; set; }

    public string? NameClaimType { get; set; }

    public string? RoleClaimType { get; set; }

    public IDictionary<string, string?> Properties { get; set; }

    public static explicit operator AuthenticationTicket(AuthenticationTicketHolder h)
    {
        var identity = new ClaimsIdentity(h.Claims.Select(x => (Claim)x), h.AuthenticationType, h.NameClaimType,
            h.RoleClaimType);
        var principal = new ClaimsPrincipal(identity);
        var properties = new AuthenticationProperties(h.Properties);
        return new AuthenticationTicket(principal, properties, h.AuthenticationScheme);
    }
}