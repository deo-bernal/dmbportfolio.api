# Build from repo root (parent of dmb.api/)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Directory.Packages.props", "./"]
COPY ["dmb.api/dmb.api.csproj", "dmb.api/"]
COPY ["dmb.data/dmb.data.csproj", "dmb.data/"]
COPY ["dmb.service/dmb.service.csproj", "dmb.service/"]
COPY ["dmb.model/dmb.model.csproj", "dmb.model/"]
RUN dotnet restore "dmb.api/dmb.api.csproj"
COPY . .
RUN dotnet publish "dmb.api/dmb.api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
# Render sets PORT; bind Kestrel to it
CMD ASPNETCORE_URLS=http://0.0.0.0:$PORT dotnet dmb.api.dll
