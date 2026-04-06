FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/TicketingEngine.API/TicketingEngine.API.csproj"
RUN dotnet publish "src/TicketingEngine.API/TicketingEngine.API.csproj" \
    -c Release -o /app/publish \
    -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_GCConserveMemory=9
ENV DOTNET_GCHeapHardLimit=400000000
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "TicketingEngine.API.dll"]