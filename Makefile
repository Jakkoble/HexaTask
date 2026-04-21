.PHONY: setup start

setup:
	@if [ ! -d jobs ]; then \
		mkdir -p jobs; \
		printf '%s\n' \
			'name: demo-job' \
			'commands:' \
			'  - echo "Hello from HexaTask"' \
			'  - echo "Working directory:"' \
			'  - pwd' \
			> jobs/demo.yaml; \
	fi

start: setup
	@set -e; \
	trap 'docker compose down' EXIT; \
	docker compose pull commander tui; \
	docker compose up -d commander; \
	docker compose run --rm --use-aliases tui
