FROM microsoft/dotnet:2.2-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src
COPY Backend.sln ./
COPY TradingBackend/TradingBackend.csproj TradingBackend/
RUN dotnet restore -nowarn:msb3202,nu1503
COPY . .
WORKDIR /src/TradingBackend
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "TradingBackend.dll"]
