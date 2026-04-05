FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore "src/TicketingEngine.API/TicketingEngine.API.csproj"

RUN dotnet publish "src/TicketingEngine.API/TicketingEngine.API.csproj" \
    -c Release -o /app/publish \
    --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN addgroup --system appgroup \
 && adduser  --system --ingroup appgroup appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "TicketingEngine.API.dll"]