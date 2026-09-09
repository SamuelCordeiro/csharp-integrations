---
name: csharp-integrations-standards
description: Applies the project's coding, documentation, and testing standards. Use when creating, modifying, reviewing, or testing code in this repository.
---

- Todas os comentários deve ser enxutos e em inglês
- Todas as propriedades, variáveis, funções, classes devem ser em inglês e seguir princípios de clean code
- Todos os ajustes e novas implementações devem ter os seus respectivos testes

## Documentation and API contracts

- Public APIs must have concise XML documentation in English.
- New or changed API endpoints must be documented in Swagger and declare expected HTTP response codes.
- API errors must use ProblemDetails and must not expose internal details outside the Development environment.
- Update README.md when a change affects setup, configuration, or public behavior.

## Security and configuration

- Never commit secrets. Use User Secrets for local development and environment variables or a secret manager in production.
- Do not weaken token validation, authorization, redirect validation, rate limiting, or CORS without an explicit requirement and a security review.

## Structure and testing

- Group service registrations and middleware in Program.cs by context using regions.
- Keep functions focused, avoid duplicated logic, and prefer explicit, meaningful names over abbreviations.
- Tests must be deterministic and must not depend on external services, credentials, or network availability.
- Test names must describe the scenario and expected result.
- Run relevant tests after every code change; do not consider an implementation complete while its tests fail.
