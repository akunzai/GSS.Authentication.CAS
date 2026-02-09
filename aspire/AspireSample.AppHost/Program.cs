var builder = DistributedApplication.CreateBuilder(args);

// Ensure Keycloak is accessible via HTTPS on port 8443.
// Aspire's proxy will handle the TLS termination (HTTPS) on the host
// and forward to Keycloak's port 8080 (HTTP) inside the container.
var keycloak = builder.AddContainer("keycloak", "quay.io/keycloak/keycloak")
    .WithImageTag("latest")
    .WithDockerfile("../../.devcontainer/keycloak")
    .WithBindMount("../../.devcontainer/keycloak/import", "/opt/keycloak/data/import")
    .WithArgs("start-dev", "--import-realm") 
    .WithHttpsEndpoint(port: 8443, targetPort: 8080, name: "keycloak-tls");

var keycloakUrl = "https://auth.dev.local:8443";

// Dedicated Aspire Integration Sample
builder.AddProject<Projects.AspireSample>("aspire-sample")
    .WaitFor(keycloak)
    .WithEnvironment("CAS__ServerUrlBase", $"{keycloakUrl}/realms/demo/protocol/cas")
    .WithEnvironment("OIDC__Authority", $"{keycloakUrl}/realms/demo")
    .WithEnvironment("SAML2__IdP__EntityId", $"{keycloakUrl}/realms/demo")
    .WithEnvironment("SAML2__IdP__MetadataLocation", $"{keycloakUrl}/realms/demo/protocol/saml/descriptor");

builder.Build().Run();
