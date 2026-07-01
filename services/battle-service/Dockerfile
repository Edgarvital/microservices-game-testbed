FROM golang:1.23-alpine AS build

WORKDIR /src

COPY go.mod ./
RUN go mod download

COPY . .
RUN go mod tidy
RUN go build -o /out/battle-service ./cmd/battle-service

FROM alpine:3.20

WORKDIR /app
COPY --from=build /out/battle-service /app/battle-service

EXPOSE 8080
ENTRYPOINT ["/app/battle-service"]
