API_URL ?= http://localhost:5050
PROJECT := src/Conduit/Conduit.csproj
LOCAL_DB ?= $(CURDIR)/conduit-local.db

# Explicitly supplied keys take precedence over the persisted local-development key.
export Jwt__SigningKey
define WITH_LOCAL_JWT
@set -e; \
if [ -z "$$Jwt__SigningKey" ]; then \
	if [ ! -s .jwt-signing-key ]; then \
		umask 077; \
		openssl rand -base64 32 > .jwt-signing-key; \
	fi; \
	export Jwt__SigningKey="$$(cat .jwt-signing-key)"; \
fi;
endef

build:
	$(WITH_LOCAL_JWT) docker compose build
run:
	$(WITH_LOCAL_JWT) docker compose up

# fetch the RealWorld API spec (hurl + bruno test collections)
submodule:
	git submodule update --init realworld

run-local:
	$(WITH_LOCAL_JWT) ASPNETCORE_URLS=$(API_URL) ConnectionStrings__Conduit="$${ConnectionStrings__Conduit:-Data Source=$(LOCAL_DB)}" dotnet run --project $(PROJECT)

# API spec tests against an already running server (make run-local in another terminal)
test-hurl:
	HOST=$(API_URL) realworld/specs/api/run-api-tests-hurl.sh

test-bruno:
	HOST=$(API_URL) bash scripts/run-bruno-tests.sh

# API spec tests managing the server themselves (used by CI)
test-hurl-with-managed-server:
	API_URL=$(API_URL) bash scripts/run-managed-api-tests.sh realworld/specs/api/run-api-tests-hurl.sh

test-bruno-with-managed-server:
	API_URL=$(API_URL) bash scripts/run-managed-api-tests.sh scripts/run-bruno-tests.sh

.PHONY: build run submodule run-local test-hurl test-bruno test-hurl-with-managed-server test-bruno-with-managed-server
