using System.Threading;
using System.Threading.Tasks;
using GSS.Authentication.CAS.Security;

namespace GSS.Authentication.CAS.Validation
{

    public interface IServiceTicketValidator
    {
        /// <summary>
        /// Validate ticket to get principal
        /// </summary>
        /// <param name="ticket"></param>
        /// <param name="service"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="System.Security.Authentication.AuthenticationException"></exception>
        /// <exception cref="System.Net.Http.HttpRequestException"></exception>
        /// <exception cref="System.UriFormatException"></exception>
        Task<ICasPrincipal> ValidateAsync(string ticket, string service, CancellationToken cancellationToken);
    }
}
