using System;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Provider;

namespace GSS.Authentication.CAS.Owin
{
    /// <summary>
    /// Provides failure context information.
    /// </summary>
    public class CasRemoteFailureContext : BaseContext
    {
        public CasRemoteFailureContext(IOwinContext context, Exception failure) : base(context)
        {
            Failure = failure;
        }

        /// <summary>
        /// User friendly error message for the error.
        /// </summary>
        public Exception Failure { get; set; }

        /// <summary>
        /// Additional state values for the authentication session.
        /// </summary>
        public AuthenticationProperties? Properties { get; set; }

        /// <summary>
        /// Indicates that stage of authentication was directly handled by
        /// user intervention and no further processing should be attempted.
        /// </summary>
        internal bool Handled { get; private set; }

        /// <summary>
        /// Indicates that the default authentication logic should be
        /// skipped and that the rest of the pipeline should be invoked.
        /// </summary>
        internal bool Skipped { get; private set; }

        /// <summary>
        /// Discontinue all processing for this request and return to the client.
        /// The caller is responsible for generating the full response.
        /// </summary>
        public void HandleResponse() => Handled = true;

        /// <summary>
        /// Discontinue processing the request in the current handler.
        /// </summary>
        public void SkipHandler() => Skipped = true;
    }
}
