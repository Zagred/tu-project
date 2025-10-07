#!/bin/bash
set -e

# Update package lists
sudo apt-get update -y

# Install Java, unzip, wget, curl, git
sudo apt-get install -y openjdk-17-jdk unzip wget curl git

# Install Gradle
GRADLE_VERSION=8.7
wget https://services.gradle.org/distributions/gradle-${GRADLE_VERSION}-bin.zip -P /tmp
sudo unzip -d /opt /tmp/gradle-${GRADLE_VERSION}-bin.zip
echo "export PATH=\$PATH:/opt/gradle-${GRADLE_VERSION}/bin" | sudo tee /etc/profile.d/gradle.sh

# Android SDK installation
ANDROID_HOME=/opt/android-sdk
sudo mkdir -p $ANDROID_HOME/cmdline-tools
wget https://dl.google.com/android/repository/commandlinetools-linux-11076708_latest.zip -O /tmp/cmdline-tools.zip
sudo unzip /tmp/cmdline-tools.zip -d $ANDROID_HOME/cmdline-tools
sudo mv $ANDROID_HOME/cmdline-tools/cmdline-tools $ANDROID_HOME/cmdline-tools/latest

# Accept Android SDK licenses
sudo yes | $ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager --licenses

# Install platforms, build-tools, and platform-tools
sudo $ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager \
    "platforms;android-34" \
    "build-tools;34.0.0" \
    "platform-tools"

# Set Android SDK environment variables for all users
echo "export ANDROID_HOME=$ANDROID_HOME" | sudo tee /etc/profile.d/android.sh
echo "export ANDROID_SDK_ROOT=$ANDROID_HOME" | sudo tee -a /etc/profile.d/android.sh
echo "export PATH=\$PATH:$ANDROID_HOME/platform-tools:$ANDROID_HOME/cmdline-tools/latest/bin" | sudo tee -a /etc/profile.d/android.sh

# Apply environment variables for the current shell
source /etc/profile.d/android.sh
source /etc/profile.d/gradle.sh

echo "App VM setup complete!"
echo "ANDROID_HOME=$ANDROID_HOME"
echo "Gradle version: $(gradle -v | grep Gradle)"
