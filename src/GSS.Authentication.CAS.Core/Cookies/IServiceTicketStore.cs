using System.Threading.Tasks;

namespace GSS.Authentication.CAS
{
    /// <summary>
    /// preserve service ticket for single sign-out
    /// </summary>
    public interface IServiceTicketStore
    {
        Task<string> StoreAsync(ServiceTicket ticket);
        Task RenewAsync(string key, ServiceTicket ticket);
        Task<ServiceTicket> RetrieveAsync(string key);
        Task RemoveAsync(string key);
    }
}
