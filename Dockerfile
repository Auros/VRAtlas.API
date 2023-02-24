# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY *.sln .
COPY VRAtlas/*.csproj ./VRAtlas/
COPY VRAtlas.Core/*.csproj ./VRAtlas.Core/
COPY VRAtlas.Tests.Unit/*.csproj ./VRAtlas.Tests.Unit/
COPY VRAtlas.Tests.Integration/*.csproj ./VRAtlas.Tests.Integration/

RUN dotnet restore

# copy everything else and build app
COPY VRAtlas/. ./VRAtlas/
COPY VRAtlas.Core/. ./VRAtlas.Core/
COPY VRAtlas.Tests.Unit/. ./VRAtlas.Tests.Unit/
COPY VRAtlas.Tests.Integration/. ./VRAtlas.Tests.Integration/

WORKDIR /app/VRAtlas
RUN dotnet publish -c Release -o out 

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build /app/VRAtlas/out ./
ENTRYPOINT ["dotnet", "VRAtlas.dll"]