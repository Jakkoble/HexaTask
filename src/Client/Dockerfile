FROM rust:1.90-bookworm AS builder
WORKDIR /repo/src/Client

RUN apt-get update \
    && apt-get install -y --no-install-recommends protobuf-compiler \
    && rm -rf /var/lib/apt/lists/*

COPY contracts /repo/contracts
COPY src/Client /repo/src/Client

RUN cargo build --release

FROM debian:bookworm-slim AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates \
    && rm -rf /var/lib/apt/lists/*

COPY --from=builder /repo/src/Client/target/release/client /usr/local/bin/client

ENV COMMANDER_ADDR=http://commander:5271
ENV JOB_DIR=/app/jobs

ENTRYPOINT ["client"]
