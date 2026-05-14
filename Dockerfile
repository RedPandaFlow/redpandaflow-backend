FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY ["redpandaflow-backend/src/RedPandaFlow.Api/RedPandaFlow.Api.csproj", "redpandaflow-backend/src/RedPandaFlow.Api/"]
COPY ["redpandaflow-backend/src/RedPandaFlow.Application/RedPandaFlow.Application.csproj", "redpandaflow-backend/src/RedPandaFlow.Application/"]
COPY ["redpandaflow-backend/src/RedPandaFlow.Domain/RedPandaFlow.Domain.csproj", "redpandaflow-backend/src/RedPandaFlow.Domain/"]
COPY ["redpandaflow-backend/src/RedPandaFlow.Infrastructure/RedPandaFlow.Infrastructure.csproj", "redpandaflow-backend/src/RedPandaFlow.Infrastructure/"]

RUN dotnet restore "redpandaflow-backend/src/RedPandaFlow.Api/RedPandaFlow.Api.csproj"

COPY . .
RUN dotnet publish "/app/redpandaflow-backend/src/RedPandaFlow.Api/RedPandaFlow.Api.csproj" -c Release -o /app/out


FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

ARG BACKEND_PORT
ENV ASPNETCORE_HTTP_PORTS=${BACKEND_PORT}

COPY --from=build /app/out .
EXPOSE ${BACKEND_PORT}
ENTRYPOINT ["dotnet", "RedPandaFlow.Api.dll"]