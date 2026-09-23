FROM mcr.microsoft.com/dotnet/sdk:8.0 AS dotnet
WORKDIR /src
COPY src/FileGmail.Api/FileGmail.Api.csproj src/FileGmail.Api/
RUN dotnet restore src/FileGmail.Api/FileGmail.Api.csproj

FROM node:22-alpine AS frontend
WORKDIR /app
COPY frontend/file-gmail-web/package*.json ./
RUN npm ci
COPY frontend/file-gmail-web ./
RUN npm run build

FROM dotnet AS publish
WORKDIR /src
COPY . .
RUN dotnet publish src/FileGmail.Api/FileGmail.Api.csproj -c Release -o /out
COPY --from=frontend /app/dist /out/wwwroot/

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=publish /out .
EXPOSE 8080
ENTRYPOINT ["dotnet", "FileGmail.Api.dll"]