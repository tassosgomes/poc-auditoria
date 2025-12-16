FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar arquivos de projeto e restaurar dependências
COPY ["src/MsAuditoria.API/MsAuditoria.API.csproj", "MsAuditoria.API/"]
COPY ["src/MsAuditoria.Application/MsAuditoria.Application.csproj", "MsAuditoria.Application/"]
COPY ["src/MsAuditoria.Infra/MsAuditoria.Infra.csproj", "MsAuditoria.Infra/"]
RUN dotnet restore "MsAuditoria.API/MsAuditoria.API.csproj"

# Copiar código fonte e publicar
COPY src/ .
RUN dotnet publish "MsAuditoria.API/MsAuditoria.API.csproj" -c Release -o /app

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

EXPOSE 5001
ENV ASPNETCORE_URLS=http://+:5001

ENTRYPOINT ["dotnet", "MsAuditoria.API.dll"]
