# https://github.com/keycloak/keycloak/releases
ARG KEYCLOAK_VERSION=26.3.2
ARG KEYCLOAK_CAS_VERSION=26.3.2
FROM quay.io/keycloak/keycloak:${KEYCLOAK_VERSION}
ARG KEYCLOAK_VERSION
ARG KEYCLOAK_CAS_VERSION

# add CAS protocol for Keycloak
# https://github.com/jacekkow/keycloak-protocol-cas
ADD --chown=keycloak:keycloak https://github.com/jacekkow/keycloak-protocol-cas/releases/download/${KEYCLOAK_CAS_VERSION}/keycloak-protocol-cas-${KEYCLOAK_CAS_VERSION}.jar /opt/keycloak/providers/keycloak-protocol-cas.jar

WORKDIR /opt/keycloak