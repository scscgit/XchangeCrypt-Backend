FROM microsoft/dotnet:2.2-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src
COPY Backend.sln ./
COPY ConvergenceBackend/ConvergenceBackend.csproj ConvergenceBackend/
COPY TradingBackend/TradingBackend.csproj TradingBackend/
COPY ConstantsLibrary/ConstantsLibrary.csproj ConstantsLibrary/
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
