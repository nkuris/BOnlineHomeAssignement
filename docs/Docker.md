# Docker Compose local development

This project includes Dockerfiles and a docker-compose.yml to run a local development environment with SQL Server, the ASP.NET server, and the SPA client.

Prerequisites
- Docker Desktop installed and running
- (Optional) SSMS or Azure Data Studio to connect to SQL Server

Setup
1. Copy `.env.example` to `.env` and set your MSSQL SA password:

   cp .env.example .env
   # then edit .env and set a secure MSSQL_SA_PASSWORD value

2. Start SQL Server container (recommended to start DB first):

   docker compose up -d mssql

   Wait for the container to become healthy (check `docker ps` and `docker logs ps_mssql`).

3. Start the server and client (build images if needed):

   docker compose up --build

4. Connect with SSMS / Azure Data Studio
- Server: localhost,1433
- Authentication: SQL Login
- Username: sa
- Password: value you set in .env

Database migrations
- To apply EF Core migrations from the host:

There are two supported ways to apply migrations. Recommended: use the provided init script

1) Helper script (recommended on Windows):

   powershell -ExecutionPolicy Bypass -File .\tools\init-db.ps1

   This will:
   - start the mssql container
   - wait for TCP 1433 to be reachable
   - run the `migrator` one-shot service which executes `dotnet ef database update`
   - bring up the server and client containers and wait for /health to report Healthy

2) Manual (SDK on host or container):

   dotnet tool restore
   dotnet ef database update --project BOnlineHomeAssignement.Server --startup-project BOnlineHomeAssignement.Server

Notes
- Do not commit your `.env` to source control. `.env.example` is provided as a template.
- For development, docker-compose.override.yml mounts source code into the server and client containers and runs `dotnet watch` and `npm run dev` respectively.
- In production, use a managed Azure SQL instance and a secure key management solution for secrets.
