FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine AS build
WORKDIR /app
COPY . ./
RUN dotnet publish "LogShark/LogShark.csproj" -r linux-x64 --self-contained false -c Release /p:Version=4.2.2 -o out

FROM mcr.microsoft.com/dotnet/core/runtime:3.1
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "LogShark.dll"]