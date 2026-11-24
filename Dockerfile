FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Stockly.slnx ./
COPY src ./src
RUN dotnet restore Stockly.slnx
RUN dotnet publish src/Stockly.Api/Stockly.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
RUN groupadd --system stockly \
    && useradd --system --gid stockly --home-dir /nonexistent --shell /usr/sbin/nologin stockly \
    && mkdir -p /var/stockly/files \
    && chown -R stockly:stockly /var/stockly/files
COPY --from=build /app/publish .
USER stockly
EXPOSE 8080
ENTRYPOINT ["dotnet", "Stockly.Api.dll"]
