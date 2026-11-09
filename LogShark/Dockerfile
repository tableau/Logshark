FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim
WORKDIR /app
COPY . ./

RUN DEBIAN_FRONTEND=noninteractive \
&& apt-get update \
&& apt-get upgrade -y -qq --allow-unauthenticated \
        -o Dpkg::Options::="--force-confold" -y \
&& apt-get install -y --no-install-recommends awscli \
    libc6 \
    libgdiplus \
    && rm -rf /var/lib/apt/lists/*

RUN dotnet publish "LogShark/LogShark.csproj" -r linux-x64 --self-contained true -c Release /p:Version=4.2.5 /p:UpdateConfigurationSchema=true -o out

WORKDIR /app

ENTRYPOINT ["/bin/bash"]
CMD ["-c", "sleep infinity"]
