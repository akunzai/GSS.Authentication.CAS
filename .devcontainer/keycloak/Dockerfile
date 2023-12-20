# https://www.keycloak.org/server/containers
ARG KEYCLOAK_VERSION=23.0.3

FROM alpine as downloader
ARG KEYCLOAK_VERSION
RUN set -eux; \
    apk add curl; \
    # https://github.com/jacekkow/keycloak-protocol-cas
    curl -Lo /tmp/keycloak-protocol-cas.jar https://github.com/jacekkow/keycloak-protocol-cas/releases/download/${KEYCLOAK_VERSION}/keycloak-protocol-cas-${KEYCLOAK_VERSION}.jar;

# https://github.com/keycloak/keycloak/tree/main/quarkus/container
# https://quay.io/repository/keycloak/keycloak?tab=tags
FROM quay.io/keycloak/keycloak:${KEYCLOAK_VERSION}

# add CAS protocol for Keycloak
COPY --from=downloader --chown=keycloak:keycloak /tmp/keycloak-protocol-cas.jar /opt/keycloak/providers/

RUN /opt/keycloak/bin/kc.sh build