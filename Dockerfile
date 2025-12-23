FROM ubuntu:22.04

ARG DEBIAN_FRONTEND=noninteractive
ARG ANDROID_SDK_ROOT=/opt/android-sdk
ARG CMDLINE_TOOLS_ZIP=commandlinetools-linux-11076708_latest.zip
ARG SONAR_SCANNER_VERSION=8.0.1.6346

RUN apt-get update && apt-get install -y \
    openjdk-17-jdk \
    wget unzip git ca-certificates \
    libc6 libstdc++6 zlib1g \
  && rm -rf /var/lib/apt/lists/*

ENV ANDROID_SDK_ROOT=${ANDROID_SDK_ROOT}
ENV PATH=${PATH}:${ANDROID_SDK_ROOT}/cmdline-tools/latest/bin:${ANDROID_SDK_ROOT}/platform-tools

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

RUN cd /opt \
 && wget -q https://binaries.sonarsource.com/Distribution/sonar-scanner-cli/sonar-scanner-cli-${SONAR_SCANNER_VERSION}-linux-x64.zip \
 && unzip -q sonar-scanner-cli-${SONAR_SCANNER_VERSION}-linux-x64.zip \
 && ln -sf /opt/sonar-scanner-${SONAR_SCANNER_VERSION}-linux-x64/bin/sonar-scanner /usr/local/bin/sonar-scanner \
 && rm -f sonar-scanner-cli-${SONAR_SCANNER_VERSION}-linux-x64.zip

RUN useradd -m -u 1000 ci
USER ci
WORKDIR /workspace
