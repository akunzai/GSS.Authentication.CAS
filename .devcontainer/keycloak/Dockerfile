# https://www.keycloak.org/server/containers
ARG KEYCLOAK_VERSION=26.1.0

# https://github.com/keycloak/keycloak/tree/main/quarkus/container
# https://quay.io/repository/keycloak/keycloak?tab=tags
FROM quay.io/keycloak/keycloak:${KEYCLOAK_VERSION}
ARG KEYCLOAK_VERSION

# add CAS protocol for Keycloak
# https://github.com/jacekkow/keycloak-protocol-cas
ADD --chown=keycloak:keycloak https://github.com/jacekkow/keycloak-protocol-cas/releases/download/${KEYCLOAK_VERSION}/keycloak-protocol-cas-${KEYCLOAK_VERSION}.jar /opt/keycloak/providers/keycloak-protocol-cas.jar

WORKDIR /opt/keycloak

RUN /opt/keycloak/bin/kc.sh build