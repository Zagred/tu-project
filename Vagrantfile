Vagrant.configure("2") do |config|
  BOX_NAME = "ubuntu/jammy64"  # Upgraded to Ubuntu 22.04
  config.vm.boot_timeout = 900

  # Share the userdata folder with all VMs
  config.vm.synced_folder "userdata", "/vagrant_userdata"

  # --- VM2: Nexus ---
  config.vm.define "nexus" do |vm|
    vm.vm.box = BOX_NAME
    vm.vm.network "private_network", ip: "192.168.56.101"
    vm.vm.provider "virtualbox" do |vb|
      vb.memory = "4096"
      vb.cpus = 2
    end
    vm.vm.provision "shell", path: "userdata/nexus-setup.sh"
  end

  # --- VM3: SonarQube ---
  config.vm.define "sonarqube" do |vm|
    vm.vm.box = BOX_NAME
    vm.vm.network "private_network", ip: "192.168.56.102"
    vm.vm.provider "virtualbox" do |vb|
      vb.memory = "6144"
      vb.cpus = 2
    end
    vm.vm.provision "shell", path: "userdata/sonar-setup.sh"
  end

  # --- VM1: Jenkins ---
  config.vm.define "jenkinsvm" do |vm|
    vm.vm.box = BOX_NAME
    vm.vm.network "private_network", ip: "192.168.56.103"
    vm.vm.provider "virtualbox" do |vb|
      vb.memory = "4096"
      vb.cpus = 2
    end
    vm.vm.provision "shell", path: "userdata/jenkins-setup.sh"
  end

    # --- VM4: App ---
  config.vm.define "app" do |vm|
    vm.vm.box = BOX_NAME
    vm.vm.network "private_network", ip: "192.168.56.104"
    vm.vm.provider "virtualbox" do |vb|
      vb.memory = "4096"
      vb.cpus = 2
    end
    vm.vm.provision "shell", path: "userdata/app-setup.sh"
  end
end
