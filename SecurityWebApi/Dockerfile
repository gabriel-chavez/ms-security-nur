#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["SecurityWebApi/SecurityWebApi.csproj", "SecurityWebApi/"]
COPY ["Security.Infrastructure/Security.Infrastructure.csproj", "Security.Infrastructure/"]
COPY ["Security.Application/Security.Application.csproj", "Security.Application/"]
RUN dotnet restore "SecurityWebApi/SecurityWebApi.csproj"
COPY . .
WORKDIR "/src/SecurityWebApi"
RUN dotnet build "SecurityWebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SecurityWebApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SecurityWebApi.dll"]