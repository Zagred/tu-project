# TU Project Deployment

This repository contains the BankApp application plus a small CI/CD lab built with Vagrant, Jenkins, SonarQube, Nexus, Docker Hub, Docker Compose, and SQL Server.

## Architecture

```text
GitHub
  -> Jenkins VM
       -> builds API and Web
       -> scans with SonarQube
       -> uploads tar.gz and APK artifacts to Nexus
       -> builds Docker images
       -> pushes Docker images to Docker Hub
       -> deploys API and Web to the App VM

App VM
  -> Docker Compose
       -> bankapp-api container on port 7083
       -> bankapp-web container on port 5000

DB VM
  -> SQL Server container on port 1433

Android Emulator
  -> calls API at http://<app-vm-host>:7083/
```

## VMs

| VM | Host value | Purpose |
| --- | --- | --- |
| nexus | `bankapp-nexus-host` Jenkins credential | Nexus artifact repository |
| sonarqube | `bankapp-sonar-host` Jenkins credential | Code analysis |
| jenkinsvm | local Vagrant host | CI/CD pipeline |
| app | `bankapp-app-vm-host` Jenkins credential | API and Web runtime |
| db | inside `bankapp-db-connection-string` Jenkins credential | SQL Server runtime |

## Required Jenkins Credentials

Create these in Jenkins before running the pipeline:

| ID | Type | Purpose |
| --- | --- | --- |
| vagrant-login | Username with password | SSH to Vagrant VMs |
| nexus-login | Username with password | Upload artifacts to Nexus |
| sonartoken | Secret text | SonarQube analysis token |
| docker-hub-credentials | Username with password | Docker Hub push and pull |
| bankapp-db-connection-string | Secret text | SQL Server connection string for API containers |
| bankapp-app-vm-host | Secret text | App VM host or IP |
| bankapp-sonar-host | Secret text | SonarQube host or IP |
| bankapp-nexus-host | Secret text | Nexus host or IP |

Example value for `bankapp-db-connection-string`:

```text
Server=<db-vm-host>,1433;Database=BankAppDb;User Id=sa;Password=<password>;TrustServerCertificate=True;
```

## Local Environment File

Copy `.env.example` to `.env` for manual Docker Compose usage:

```powershell
Copy-Item .env.example .env
```

Update the password values in `.env`. The `.env` file is ignored by Git.

## Common Commands

Start only the runtime VMs:

```powershell
vagrant up db app --no-provision
```

Provision the DB VM with a password from the host environment:

```powershell
$env:BANKAPP_DB_PASSWORD = "<password>"
vagrant provision db
```

Deploy API and Web manually on the App VM from local source:

```powershell
vagrant ssh app -c "sudo env APP_ROOT=/vagrant BANKAPP_DB_CONNECTION_STRING='Server=<db-vm-host>,1433;Database=BankAppDb;User Id=sa;Password=<password>;TrustServerCertificate=True;' bash /vagrant_userdata/app-docker-deploy.sh"
```

Check runtime endpoints:

```powershell
curl.exe http://<app-vm-host>:7083/api/users/testuser
curl.exe http://<app-vm-host>:5000/
```

Build and install the Android debug APK locally:

```powershell
dotnet build BankApp\BankAPP\BankAPP.csproj -f net10.0-android -c Debug -p:EmbedAssembliesIntoApk=true
& "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe" install --no-incremental -r BankApp\BankAPP\bin\Debug\net10.0-android\com.companyname.bankapp-Signed.apk
```

## Artifacts

Jenkins uploads these to Nexus:

```text
web-api/<BUILD_NUMBER>/bankapi-<BUILD_NUMBER>.tar.gz
web-api/<BUILD_NUMBER>/bankweb-<BUILD_NUMBER>.tar.gz
mobile/<BUILD_NUMBER>/bankapp-mobile-<BUILD_NUMBER>.apk
```

Jenkins pushes these Docker images to Docker Hub:

```text
pacopandev/bankapp-api:<BUILD_NUMBER>
pacopandev/bankapp-api:latest
pacopandev/bankapp-web:<BUILD_NUMBER>
pacopandev/bankapp-web:latest
```

## Notes

OpenShift Sandbox is not part of the final deployment path. The Microsoft SQL Server container requires security permissions that the sandbox does not provide, so the stable deployment target remains the VM lab.
