# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-06-06

### Added

- RabbitMQ.Client 7.x abstraction with async APIs
- `Microsoft.Extensions.DependencyInjection` integration via `AddRabbitFlow()`
- `IPublisher` and `ITypedPublisher<T>` for message publishing
- `Consumer<T>` with `ConsumerHostedService` for background consumption
- Channel pool for publishing and dedicated channels for consuming
- Topology declaration (exchanges, queues, and bindings)
- `JsonMessageSerializer` as the default serializer
- Automatic connection recovery
- Sample projects (publisher and consumer)
- Unit tests with over 80% code coverage

[1.0.0]: https://github.com/DanielDxD/rabbit-flow/releases/tag/v1.0.0
