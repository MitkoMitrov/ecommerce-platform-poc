# Project goal

Build a focused e-commerce Cart proof of concept using:

- .NET 10
- ASP.NET Core Minimal APIs
- EF Core
- PostgreSQL
- React
- TypeScript
- RTK Query
- request validation
- ProblemDetails
- structured logging
- health checks
- unit tests
- PostgreSQL integration tests
- Docker Compose
- CI checks
- README documentation

# Proof-of-concept scope

The repository will demonstrate one complete Cart vertical slice.

The proof of concept is intentionally smaller than the separately documented target production architecture.

# Explicit scope exclusions

Do not implement unless a future externally reviewed prompt explicitly requests it:

- authentication or authorization
- Keycloak
- BFF
- Redis
- RabbitMQ
- outbox or inbox
- Elasticsearch
- Kubernetes
- Terraform
- checkout
- payments
- orders
- inventory reservations
- mobile application
- microservices
- event sourcing
- generic repository abstractions
- generic service abstractions
- generic handler base classes
- custom Result frameworks
- AutoMapper

# Engineering rules

- Use stable .NET 10 and target net10.0.
- Use ASP.NET Core Minimal APIs, not controllers.
- Keep code explicit and proportionate to the proof of concept.
- Nullable reference types must be enabled.
- Compiler warnings must be treated as errors.
- Do not suppress warnings without external approval.
- Do not add packages without explaining why they are required.
- Do not create speculative abstractions.
- Do not create placeholder, fake, or empty tests.
- Do not change the agreed scope without approval.
- Inspect existing files before modifying them.
- Run all verification commands required by the current milestone.
- Never claim that a command succeeded unless it was actually executed.
- Report failed commands honestly.
- Stop after every milestone and wait for external review.

# Incremental milestone workflow

For every milestone:

1. Read this CLAUDE.md file.
2. Inspect the existing repository state.
3. Complete only the explicitly requested work.
4. Run the required verification commands.
5. Fix in-scope failures where possible.
6. Produce the mandatory report.
7. Stop and wait for external review.

Do not continue to another milestone automatically.

# Verification requirements

- Builds must complete with zero errors and zero compiler warnings.
- Tests must actually be executed when test projects exist.
- Do not report tests as passing unless the command was run.
- Report exact commands and results.
- Report known limitations and unresolved issues.
- Do not hide failed commands or warnings.

# Git rules

- Do not run `git add`.
- Do not stage files.
- Do not create commits.
- Do not amend commits.
- Do not rewrite Git history.
- Suggest a commit message only after verification.
- The user creates commits after external review.

# Mandatory completion report

Every milestone report must use these headings in this exact order:

1. Milestone status
2. Completed work
3. Files created
4. Files modified
5. Files removed
6. Technical decisions
7. Packages added
8. Commands executed
9. Verification results
10. Git state
11. Known limitations or remaining issues
12. Questions requiring approval
13. Suggested commit message

Milestone status must be exactly one of:

- COMPLETED
- BLOCKED
- FAILED
