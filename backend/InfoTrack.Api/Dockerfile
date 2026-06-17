FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY InfoTrack.sln ./
COPY InfoTrack.Domain/InfoTrack.Domain.csproj InfoTrack.Domain/
COPY InfoTrack.Application/InfoTrack.Application.csproj InfoTrack.Application/
COPY InfoTrack.Infrastructure/InfoTrack.Infrastructure.csproj InfoTrack.Infrastructure/
COPY InfoTrack.Api/InfoTrack.Api.csproj InfoTrack.Api/
RUN dotnet restore InfoTrack.Api/InfoTrack.Api.csproj

COPY . .
RUN dotnet publish InfoTrack.Api/InfoTrack.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "InfoTrack.Api.dll"]
