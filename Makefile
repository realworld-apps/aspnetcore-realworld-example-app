API_URL ?= http://localhost:5000
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
	HOST=$(API_URL) bash scripts/run-bruno-tests.sh

# API spec tests managing the server themselves (used by CI)
test-hurl-with-managed-server:
	API_URL=$(API_URL) bash scripts/run-managed-api-tests.sh realworld/specs/api/run-api-tests-hurl.sh

test-bruno-with-managed-server:
	API_URL=$(API_URL) bash scripts/run-managed-api-tests.sh scripts/run-bruno-tests.sh

.PHONY: build run submodule run-local test-hurl test-bruno test-hurl-with-managed-server test-bruno-with-managed-server
