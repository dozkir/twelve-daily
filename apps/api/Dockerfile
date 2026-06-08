FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
RUN apt-get update \
	&& apt-get install -y --no-install-recommends libgssapi-krb5-2 \
	&& rm -rf /var/lib/apt/lists/*
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["TwelveDaily.Api/TwelveDaily.Api.csproj", "TwelveDaily.Api/"]
COPY ["TwelveDaily.Application/TwelveDaily.Application.csproj", "TwelveDaily.Application/"]
COPY ["TwelveDaily.Domain/TwelveDaily.Domain.csproj", "TwelveDaily.Domain/"]
COPY ["TwelveDaily.Infrastructure/TwelveDaily.Infrastructure.csproj", "TwelveDaily.Infrastructure/"]
RUN dotnet restore "TwelveDaily.Api/TwelveDaily.Api.csproj"
COPY . .
RUN dotnet publish "TwelveDaily.Api/TwelveDaily.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TwelveDaily.Api.dll"]

