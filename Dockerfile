# Используем официальный образ .NET 8 (вместо 9 — пока он в preview)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Копируем .csproj файл из корня
COPY *.csproj ./
RUN dotnet restore

# Копируем остальную часть проекта
COPY . . 
RUN dotnet publish -c Release -o out

# Runtime образ
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

# Указываем порт и точку входа
EXPOSE 8080
ENTRYPOINT ["dotnet", "ItransitionAuthentication.dll"]