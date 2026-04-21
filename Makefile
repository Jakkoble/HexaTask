.PHONY: up

up:
	@set -e; \
	trap 'docker compose down' EXIT; \
	docker compose up -d commander; \
	docker compose run --rm --use-aliases tui
