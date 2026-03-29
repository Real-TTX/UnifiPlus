FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/UnifiPlus.Web/UnifiPlus.Web.csproj", "src/UnifiPlus.Web/"]
RUN dotnet restore "src/UnifiPlus.Web/UnifiPlus.Web.csproj"

COPY . .
WORKDIR "/src/src/UnifiPlus.Web"
RUN dotnet publish "UnifiPlus.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "UnifiPlus.Web.dll"]
