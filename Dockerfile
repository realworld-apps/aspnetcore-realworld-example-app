#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.


FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet run --project build/build.csproj -- publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

WORKDIR /app
COPY --from=build /src/publish .
EXPOSE 8080

ENTRYPOINT ["dotnet", "Conduit.dll"]
