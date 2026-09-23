FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/FileGmail.Api/FileGmail.Api.csproj src/FileGmail.Api/
RUN dotnet restore src/FileGmail.Api/FileGmail.Api.csproj

COPY . .
WORKDIR /src/frontend/file-gmail-web
RUN npm ci && npm run build

WORKDIR /src
RUN dotnet publish src/FileGmail.Api/FileGmail.Api.csproj -c Release -o /app
COPY --from=build /src/frontend/file-gmail-web/dist /app/wwwroot/

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "FileGmail.Api.dll"]