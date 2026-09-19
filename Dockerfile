FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /app

COPY Zephyr.slnx ./
COPY global.json Directory.Build.props Directory.Packages.props NuGet.config* ./
COPY Crypto/Zephyr.Crypto.fsproj ./Crypto/
COPY TL/Zephyr.TL.fsproj       ./TL/
COPY Core/Zephyr.Core.fsproj   ./Core/
COPY Tests/Zephyr.Tests.fsproj ./Tests/

RUN dotnet restore Zephyr.slnx

COPY Crypto/ ./Crypto/
COPY TL/     ./TL/
COPY Core/   ./Core/
COPY Tests/  ./Tests/

RUN dotnet test Zephyr.slnx \
      --configuration Release \
      --no-restore \
      --logger "trx;LogFileName=results.trx" \
      --results-directory /app/TestResults