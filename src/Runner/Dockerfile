FROM golang:1.26.1 AS builder
WORKDIR /app

COPY go.mod go.sum ./
RUN go mod download

COPY . .

RUN CGO_ENABLED=0 GOOS=linux go build -o /app/runner ./main.go

FROM ubuntu:latest
WORKDIR /app

COPY --from=builder /app/runner .

RUN chmod +x ./runner

CMD ["./runner"]
