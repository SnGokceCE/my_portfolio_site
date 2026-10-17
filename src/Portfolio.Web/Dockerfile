# Çok aşamalı derleme: SDK imajında derle, sadece çalışma zamanı imajında çalıştır.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Önce proje dosyalarını kopyalayıp restore etmek, Docker katman önbelleğinden yararlanmayı sağlar.
COPY global.json Directory.Build.props Portfolio.slnx ./
COPY src/Portfolio.Domain/Portfolio.Domain.csproj src/Portfolio.Domain/
COPY src/Portfolio.Application/Portfolio.Application.csproj src/Portfolio.Application/
COPY src/Portfolio.Infrastructure/Portfolio.Infrastructure.csproj src/Portfolio.Infrastructure/
COPY src/Portfolio.Web/Portfolio.Web.csproj src/Portfolio.Web/
RUN dotnet restore src/Portfolio.Web/Portfolio.Web.csproj

COPY src/ src/
RUN dotnet publish src/Portfolio.Web/Portfolio.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Portfolio.Web.dll"]
