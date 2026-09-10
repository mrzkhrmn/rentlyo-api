FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Rentlyo.sln ./
COPY src/Rentlyo.API/Rentlyo.API.csproj src/Rentlyo.API/
COPY src/Rentlyo.Application/Rentlyo.Application.csproj src/Rentlyo.Application/
COPY src/Rentlyo.Domain/Rentlyo.Domain.csproj src/Rentlyo.Domain/
COPY src/Rentlyo.Infrastructure/Rentlyo.Infrastructure.csproj src/Rentlyo.Infrastructure/
COPY src/Rentlyo.Shared/Rentlyo.Shared.csproj src/Rentlyo.Shared/

RUN dotnet restore src/Rentlyo.API/Rentlyo.API.csproj

COPY src/ ./src/
RUN dotnet publish src/Rentlyo.API/Rentlyo.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Rentlyo.API.dll"]
