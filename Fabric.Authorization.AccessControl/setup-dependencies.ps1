#Requires -RunAsAdministrator
#
# setup-dependencies.ps1
#

$nodeVersion = "8.12.0"
$angularVersion = "6.1.2"
$typescriptVersion = "2.9.2"
$npmVersion = "6.4.1"
$nl = [Environment]::NewLine

# Chocolatey must be installed
If (-NOT (Test-Path -Path "$env:ProgramData\Chocolatey") ) {
  Write-Output "Chocolatey is being installed..."
  Set-ExecutionPolicy Bypass -Scope Process -Force; iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))
	If (-NOT (Test-Path -Path "$env:ProgramData\Chocolatey") ) {
    Write-Output "Chocolatey is not installed..."
    exit
  }
}

# remove node_modules
Write-Output ""
Write-Output "$nl----] Removing previous node modules [----$nl"
Remove-Item .\\node_modules -Force -Recurse


# uninstall angular/cli, typescript and node-sass
Write-Output "$nl----] Removing previous angular cli, typescript and node-sass [----$nl"
npm uninstall -g @angular/cli
npm uninstall -g typescript
npm cache clean --force
npm cache verify

# uninstall nodejs
Write-Output "$nl----] Removing previous node [----$nl"
choco uninstall nodejs.install --all-versions

# install nodejs
Write-Output "$nl----] Installing Node $nodeVersion [----$nl"
choco install nodejs.install -y --version $nodeVersion

# install npm
Write-Output "$nl----] Installing NPM $npmVersion [----$nl"
npm install -g npm@$npmVersion

# install angular cli
Write-Output "$nl----] Installing Angular CLI $angularVersion [----$nl"
npm install -g @angular/cli@$angularVersion

# install typescript cli
Write-Output "$nl----] Installing Typescript CLI $typescriptVersion [----$nl"
npm install -g typescript@$typescriptVersion

# install all dependencies
Write-Output "$nl----] Installing npm packages for this version [----$nl"
npm install

# if everything is good, here are the new versions:
Write-Output ""
Write-Output "Here are the versions of everything and what versions they should be:"
Write-Output "Node should be: $nodeVersion"
node -v
Write-Output "npm should be: $npmVersion"
npm -v
Write-Output "typescript should be: $typescriptVersion"
tsc -v
Write-Output "Angular CLI should be: $angularVersion"
ng -v
