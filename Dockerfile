FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

WORKDIR /src
COPY ["nuget.config", "./"]
COPY ["TranslationChecker/TranslationChecker.csproj", "./"]
RUN dotnet restore

COPY . ./

RUN dotnet build -c Release

RUN dotnet test

RUN dotnet publish ./TranslationChecker/TranslationChecker.csproj -c Release --no-build -o ./out


FROM mcr.microsoft.com/dotnet/core/runtime:3.1
WORKDIR /app
COPY --from=build /src/out .
ENTRYPOINT ["dotnet", "/app/TranslationChecker.dll", "/github/workspace"]