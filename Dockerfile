FROM ubuntu:22.04

ARG DEBIAN_FRONTEND=noninteractive
ARG ANDROID_SDK_ROOT=/opt/android-sdk
ARG CMDLINE_TOOLS_ZIP=commandlinetools-linux-11076708_latest.zip

RUN apt-get update && apt-get install -y \
    apt-transport-https \
    ca-certificates \
    curl \
    git \
    gnupg \
    openjdk-17-jdk \
    software-properties-common \
    unzip \
    wget \
  && rm -rf /var/lib/apt/lists/*

RUN add-apt-repository -y ppa:dotnet/backports \
 && apt-get update \
 && apt-get install -y dotnet-sdk-9.0 dotnet-sdk-10.0 \
 && rm -rf /var/lib/apt/lists/*

ENV ANDROID_SDK_ROOT=${ANDROID_SDK_ROOT}
ENV ANDROID_HOME=${ANDROID_SDK_ROOT}
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV PATH=${PATH}:${ANDROID_SDK_ROOT}/cmdline-tools/latest/bin:${ANDROID_SDK_ROOT}/platform-tools:/opt/dotnet-tools

RUN mkdir -p ${ANDROID_SDK_ROOT}/cmdline-tools \
 && cd /tmp \
 && wget -q https://dl.google.com/android/repository/${CMDLINE_TOOLS_ZIP} \
 && unzip -q ${CMDLINE_TOOLS_ZIP} -d /tmp/cmdline \
 && mv /tmp/cmdline/cmdline-tools ${ANDROID_SDK_ROOT}/cmdline-tools/latest \
 && rm -rf /tmp/*

RUN yes | sdkmanager --licenses \
 && sdkmanager --install \
    "platform-tools" \
    "platforms;android-36" \
    "build-tools;36.0.0"

RUN dotnet workload install maui-android \
 && dotnet tool install --tool-path /opt/dotnet-tools dotnet-sonarscanner

RUN useradd -m -u 1000 ci \
 && chown -R ci:ci ${ANDROID_SDK_ROOT} /opt/dotnet-tools

USER ci
WORKDIR /workspace
