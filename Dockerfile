FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY ["src/RedPandaFlow.Api/RedPandaFlow.Api.csproj", "src/RedPandaFlow.Api/"]
COPY ["src/RedPandaFlow.Application/RedPandaFlow.Application.csproj", "src/RedPandaFlow.Application/"]
COPY ["src/RedPandaFlow.Domain/RedPandaFlow.Domain.csproj", "src/RedPandaFlow.Domain/"]
COPY ["src/RedPandaFlow.Infrastructure/RedPandaFlow.Infrastructure.csproj", "src/RedPandaFlow.Infrastructure/"]

RUN dotnet restore "src/RedPandaFlow.Api/RedPandaFlow.Api.csproj"

COPY . .
RUN dotnet publish "src/RedPandaFlow.Api/RedPandaFlow.Api.csproj" \
    -c Release -o /app/out --no-restore /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

ARG BACKEND_PORT
ENV ASPNETCORE_HTTP_PORTS=${BACKEND_PORT}

COPY --from=build /app/out .
EXPOSE ${BACKEND_PORT}
USER $APP_UID
ENTRYPOINT ["dotnet", "RedPandaFlow.Api.dll"]
