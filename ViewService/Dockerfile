FROM microsoft/dotnet:2.2-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src
COPY Backend.sln ./
COPY ConvergenceService/ConvergenceService.csproj ConvergenceService/
COPY TradingService/TradingService.csproj TradingService/
COPY ViewService/ViewService.csproj ViewService/
COPY WalletService/WalletService.csproj WalletService/
COPY ConstantsLibrary/ConstantsLibrary.csproj ConstantsLibrary/
COPY DatabaseAccess/DatabaseAccess.csproj DatabaseAccess/
COPY QueueAccess/QueueAccess.csproj QueueAccess/

RUN dotnet restore -nowarn:msb3202,nu1503
COPY . .
WORKDIR /src/ViewService
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "ViewService.dll"]
