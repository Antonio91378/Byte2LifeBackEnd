FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["backend/Byte2Life.API.csproj", "backend/"]
RUN dotnet restore "backend/Byte2Life.API.csproj"

COPY . .
RUN dotnet publish "backend/Byte2Life.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 10000

ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-10000} dotnet Byte2Life.API.dll"]