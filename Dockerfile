ARG BASE_REGISTRY=mcr.microsoft.com
ARG BASE_IMAGE_TAG=10.0
FROM ${BASE_REGISTRY}/dotnet/sdk:${BASE_IMAGE_TAG} AS build-env

WORKDIR /app

COPY Zephyr.slnx ./

COPY global.json* Directory.Build.props* Directory.Packages.props* NuGet.config* ./

COPY Crypto/Zephyr.Crypto.fsproj ./Crypto/
COPY TL/Zephyr.TL.fsproj         ./TL/
COPY Core/Zephyr.Core.fsproj     ./Core/
COPY Tests/Zephyr.Tests.fsproj   ./Tests/

RUN dotnet restore Zephyr.slnx

COPY Crypto/ ./Crypto/
COPY TL/     ./TL/
COPY Core/   ./Core/
COPY Tests/  ./Tests/

RUN mkdir -p /app/TestResults && chmod -R 0777 /app/TestResults

ENTRYPOINT ["dotnet", "test", "Zephyr.slnx", \
            "--configuration", "Release", \
            "--no-restore", \
            "--logger", "trx;LogFileName=results.trx", \
            "--results-directory", "/app/TestResults"]