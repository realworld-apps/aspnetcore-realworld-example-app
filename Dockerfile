#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.


FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
WORKDIR /src
COPY . .
RUN dotnet restore Conduit.slnx --locked-mode \
    && dotnet run --project build/build.csproj --no-restore -- publish --check-format

FROM mcr.microsoft.com/dotnet/aspnet:10.0.12 AS base

WORKDIR /app
COPY --from=build /src/publish .
RUN mkdir /data && chown app:app /data
ENV Database__Provider=sqlite \
    ConnectionStrings__Conduit="Data Source=/data/realworld.db"
USER app
EXPOSE 8080

ENTRYPOINT ["dotnet", "Conduit.dll"]
