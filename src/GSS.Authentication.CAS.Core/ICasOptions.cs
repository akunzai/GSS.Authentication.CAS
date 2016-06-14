namespace GSS.Authentication.CAS
{
    public interface ICasOptions
    {
        /// <summary>
        /// The base url of the CAS server
        /// </summary>
        /// <example>https://cas.example.com/cas</example>
        string CasServerUrlBase { get; }

        string AuthenticationType { get; }
    }
}
