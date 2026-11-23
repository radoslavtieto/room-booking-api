# Production Readiness Roadmap

This roadmap captures the highest-priority work before production deployment.

## 1. Database and Persistence

- Add optimistic concurrency if overlapping writes are expected under high concurrency
- Run migrations as a deployment step for higher environemnts and keep the one at startup for dev environments.
- Move from SQLite to a managed production database; verify migration and concurrency behavior. 

## 2. API Reliability and Security

- Add authentication, authorization, HTTPS, and environment-specific host settings.
- Validate and limit requests; configure rate limits and a restrictive CORS policy.
- Add Swagger/OpenAPI documentation and health checks for the API and database.

## 3. Observability and Operations

- Introduce structured logging, metrics, and distributed tracing.
- Add monitoring and alerting for API and database health, errors, and performance.

## 4. Testing and Quality

- Run load and recovery tests against expected peak traffic.
- Add automated dependency and security scans to CI.

## 5. Deployment and Delivery

- Automate build, test, security checks, and infrastructure provisioning in CI/CD.
- Store production configuration and secrets outside the repository.

## 6. Add cloud-native features
- Add infrastructure as code (IaC) for provisioning and managing cloud resources.
- Add containerization and orchestration for deployment, scaling, and management of the application.

## 7. Introduce agentic AI development and testing (optional and if aggreed with the client)
- Introduce agentic AI to assist in development, testing, and deployment of the application.
- Add AI-driven monitoring and alerting for the application and infrastructure.
- Add relevant document for AI transparency, ethics, and governance for the application and its development process.
