namespace GSS.Authentication.CAS
{
    public class CasOptions : ICasOptions
    {
        public string AuthenticationType { get; set; } = CasDefaults.AuthenticationType;

        public string CasServerUrlBase { get; set; } = default!;
    }
}
