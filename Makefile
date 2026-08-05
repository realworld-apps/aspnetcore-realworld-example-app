API_URL := http://localhost:5000
PROJECT := src/Conduit/Conduit.csproj

build:
	docker compose build
run:
	docker compose up

# fetch the RealWorld API spec (hurl + bruno test collections)
submodule:
	git submodule update --init realworld

run-local:
	ASPNETCORE_URLS=$(API_URL) dotnet run --project $(PROJECT)

# API spec tests against an already running server (make run-local in another terminal)
test-hurl:
	HOST=$(API_URL) realworld/specs/api/run-api-tests-hurl.sh

test-bruno:
	HOST=$(API_URL) realworld/specs/api/run-api-tests-bruno.sh

# API spec tests managing the server themselves (used by CI)
test-hurl-with-managed-server:
	$(call run_with_managed_server,realworld/specs/api/run-api-tests-hurl.sh)

test-bruno-with-managed-server:
	$(call run_with_managed_server,realworld/specs/api/run-api-tests-bruno.sh)

# starts the API on a fresh database, waits for it, runs $(1), then shuts the API down
define run_with_managed_server
	rm -f src/Conduit/realworld.db; \
	ASPNETCORE_URLS=$(API_URL) dotnet run --project $(PROJECT) & \
	SERVER_PID=$$!; \
	timeout 120 bash -c 'until curl -s $(API_URL)/api/tags > /dev/null; do sleep 0.5; done'; \
	HOST=$(API_URL) $(1); \
	STATUS=$$?; \
	kill $$SERVER_PID; \
	exit $$STATUS
endef

.PHONY: build run submodule run-local test-hurl test-bruno test-hurl-with-managed-server test-bruno-with-managed-server
