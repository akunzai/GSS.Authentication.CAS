// Local parallel multi-target builds:
//   docker buildx bake -f docker-bake.hcl --load
//
// CI uses docker/build-push-action with type=gha (see .github/workflows/e2e-tests.yml).

variable "KEYCLOAK_VERSION" {
  default = "26.6.4"
}

variable "KEYCLOAK_CAS_VERSION" {
  default = "26.6.4"
}

group "default" {
  targets = ["keycloak", "dotnet"]
}

target "keycloak" {
  context    = "./keycloak"
  dockerfile = "Dockerfile"
  tags       = ["keycloak:cas"]
  args = {
    KEYCLOAK_VERSION     = KEYCLOAK_VERSION
    KEYCLOAK_CAS_VERSION = KEYCLOAK_CAS_VERSION
  }
}

target "dotnet" {
  context    = "."
  dockerfile = "Dockerfile"
  tags       = ["dotnet-sdk:latest"]
}
