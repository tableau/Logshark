FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /app
COPY . ./
RUN dotnet publish "LogShark/LogShark.csproj" -r linux-x64 --self-contained false -c Release /p:Version=4.2.5 -o out

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "LogShark.dll"]