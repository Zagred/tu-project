Vagrant.configure("2") do |config|
  BOX_NAME = "ubuntu/jammy64"  # Upgraded to Ubuntu 22.04
  config.vm.boot_timeout = 900
  config.ssh.username = "vagrant"
  config.ssh.password = "vagrant"
  config.ssh.insert_key = false
  config.ssh.keys_only = false

  stabilize_virtualbox = lambda do |vb|
    vb.customize ["modifyvm", :id, "--graphicscontroller", "vmsvga"]
    vb.customize ["modifyvm", :id, "--vram", "32"]
    vb.customize ["modifyvm", :id, "--nictype1", "virtio"]
    vb.customize ["modifyvm", :id, "--nictype2", "virtio"]
  end

  # Share the userdata folder with all VMs
  config.vm.synced_folder "userdata", "/vagrant_userdata"

  # --- VM2: Nexus ---
  config.vm.define "nexus" do |vm|
    vm.vm.box = BOX_NAME
    vm.vm.network "private_network", ip: "192.168.56.101"
    vm.vm.provider "virtualbox" do |vb|
      vb.memory = "2048"
      vb.cpus = 2
      stabilize_virtualbox.call(vb)
    end
    vm.vm.provision "shell", path: "userdata/nexus-setup.sh"
  end

  # --- VM3: SonarQube ---
  config.vm.define "sonarqube" do |vm|
    vm.vm.box = BOX_NAME
    vm.vm.network "private_network", ip: "192.168.56.102"
    vm.vm.provider "virtualbox" do |vb|
      vb.memory = "4096"
      vb.cpus = 2
      stabilize_virtualbox.call(vb)
    end
    vm.vm.provision "shell", path: "userdata/sonar-setup.sh"
  end

  # --- VM1: Jenkins ---
  config.vm.define "jenkinsvm" do |vm|
    vm.vm.box = BOX_NAME
    vm.vm.network "private_network", ip: "192.168.56.103"
    vm.vm.provider "virtualbox" do |vb|
      vb.memory = "2048"
      vb.cpus = 2
      stabilize_virtualbox.call(vb)
    end
    vm.vm.provision "shell", path: "userdata/jenkins-setup.sh"
  end

    # --- VM4: App ---
  config.vm.define "app" do |vm|
    vm.vm.box = BOX_NAME
    vm.vm.network "private_network", ip: "192.168.56.104"
    vm.vm.provider "virtualbox" do |vb|
      vb.memory = "3072"
      vb.cpus = 2
      stabilize_virtualbox.call(vb)
    end
    vm.vm.provision "shell", path: "userdata/app-setup.sh"
  end

  # --- VM5: Database ---
  config.vm.define "db" do |vm|
    vm.vm.box = BOX_NAME
    vm.vm.network "private_network", ip: "192.168.56.105"
    vm.vm.provider "virtualbox" do |vb|
      vb.memory = "2048"
      vb.cpus = 2
      stabilize_virtualbox.call(vb)
    end
    vm.vm.provision "shell", path: "userdata/db-setup.sh"
  end
end
