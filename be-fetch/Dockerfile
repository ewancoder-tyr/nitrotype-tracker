FROM golang:latest as build
WORKDIR /app

COPY go.mod go.sum ./
RUN go mod download

COPY *.go ./
RUN CGO_ENABLED=0 GOOS=linux go build -o /app/app

FROM gcr.io/distroless/base-debian12 AS release
WORKDIR /

# Copy Healthcheck executable.
COPY --from=ewancoder/healthcheck:latest /healthcheck /healthcheck
HEALTHCHECK CMD ["/healthcheck"]

COPY --from=build /app/app /app
USER nonroot:nonroot

ENTRYPOINT ["/app"]
