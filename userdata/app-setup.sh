#!/bin/bash
set -euo pipefail

export DEBIAN_FRONTEND=noninteractive

sudo dpkg --configure -a
sudo apt-get update -y
sudo apt-get install -y \
  apt-transport-https \
  ca-certificates \
  curl \
  git \
  gnupg \
  openjdk-17-jdk \
  software-properties-common \
  unzip \
  wget

sudo add-apt-repository -y ppa:dotnet/backports

sudo apt-get update -y
sudo apt-get install -y dotnet-sdk-9.0 dotnet-sdk-10.0

ANDROID_HOME=/opt/android-sdk
CMDLINE_TOOLS_ZIP=commandlinetools-linux-11076708_latest.zip

sudo mkdir -p "$ANDROID_HOME/cmdline-tools"
rm -rf /tmp/android-cmdline-tools /tmp/cmdline-tools.zip
wget "https://dl.google.com/android/repository/$CMDLINE_TOOLS_ZIP" -O /tmp/cmdline-tools.zip
unzip -q /tmp/cmdline-tools.zip -d /tmp/android-cmdline-tools
sudo rm -rf "$ANDROID_HOME/cmdline-tools/latest"
sudo mv /tmp/android-cmdline-tools/cmdline-tools "$ANDROID_HOME/cmdline-tools/latest"
rm -rf /tmp/android-cmdline-tools /tmp/cmdline-tools.zip

sudo env ANDROID_HOME="$ANDROID_HOME" ANDROID_SDK_ROOT="$ANDROID_HOME" \
  "$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager" \
  "platform-tools" \
  "platforms;android-36" \
  "build-tools;36.0.0"

set +o pipefail
yes | sudo env ANDROID_HOME="$ANDROID_HOME" ANDROID_SDK_ROOT="$ANDROID_HOME" \
  "$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager" --licenses
set -o pipefail

cat <<EOT | sudo tee /etc/profile.d/android.sh >/dev/null
export ANDROID_HOME=$ANDROID_HOME
export ANDROID_SDK_ROOT=$ANDROID_HOME
export PATH=\$PATH:$ANDROID_HOME/platform-tools:$ANDROID_HOME/cmdline-tools/latest/bin:/home/vagrant/.dotnet/tools
EOT

source /etc/profile.d/android.sh

sudo -H -u vagrant bash -lc \
  'export ANDROID_HOME=/opt/android-sdk ANDROID_SDK_ROOT=/opt/android-sdk; dotnet workload install maui-android --skip-manifest-update'
sudo -H -u vagrant bash -lc 'dotnet tool update --global dotnet-sonarscanner || dotnet tool install --global dotnet-sonarscanner'

echo "App VM setup complete!"
echo "ANDROID_HOME=$ANDROID_HOME"
dotnet --info
