#Requires -RunAsAdministrator
#
# setup-dependencies.ps1
#

# Change these values as needed.
$nodeVersion = "8.12.0"
$angularVersion = "6.1.2"
$typescriptVersion = "2.9.2"
$npmVersion = "6.4.1"
$nl = [Environment]::NewLine

# This will be the log for any application having issues
$Logfile = ".\setup-dependencies.log"

# This is to throw exceptions for any app that has issues.
$ErrorActionPreference = "Stop"

# Chocolatey must be installed
If (-NOT (Test-Path -Path "$env:ProgramData\Chocolatey") ) {
  Write-Output "Chocolatey is being installed..."
  Set-ExecutionPolicy Bypass -Scope Process -Force; iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))
	If (-NOT (Test-Path -Path "$env:ProgramData\Chocolatey") ) {
    Write-Output "Chocolatey is not installed..."
    exit
  }
}

Function LogWrite
{
   Param ([string]$logstring)

   Add-content $Logfile -value $logstring
}

function Invoke-Call {
  param (
      [scriptblock]$ScriptBlock,
      [string]$ErrorAction = $ErrorActionPreference
  )
  & @ScriptBlock
  if (($lastexitcode -ne 0) -and $ErrorAction -eq "Stop") {
      throw "The application has an exit code of: $lastexitcode"
  }
}

# remove node_modules
Write-Output ""
Write-Output "$nl----] Removing previous node modules [----$nl"
try
{
  if(Test-Path -Path .\\node_modules){
    Remove-Item .\\node_modules -Force -Recurse
  }
}
catch
{
    $ErrorMessage = $_.Exception.Message
    $ErrorTime = (Get-Date).ToString()
    LogWrite("")
    LogWrite("$ErrorTime : Error: Removing previous node modules $nl $ErrorMessage")
    Write-Output "There was an issue deleting node_modules.  Check to make sure you have access and a process isnt locking up the delete.  Also, the $Logfile will have more details."
    exit
}


# uninstall angular/cli, typescript and node-sass
Write-Output "$nl----] Removing previous angular cli, typescript and node-sass [----$nl"
try
{
  Invoke-Call -ScriptBlock
  {
    npm uninstall -g @angular/cli
    npm uninstall -g typescript
    npm cache clean --force
    npm cache verify
  }
}
catch
{
    $ErrorMessage = $_.Exception.Message
    $ErrorTime = (Get-Date).ToString()
    LogWrite("")
    LogWrite("$ErrorTime : Error: Removing previous angular cli, typescript and node-sass $nl $ErrorMessage")
    Write-Output "There was an issue removing previous angular cli, typescript and node-sass.  Chances are that you don't have anything installed and this is not an issue.  Also, the $Logfile will have more details."
}

# uninstall nodejs
Write-Output "$nl----] Removing previous node [----$nl"
try
{
  Invoke-Call -ScriptBlock { choco uninstall nodejs.install --all-versions }
}
catch
{
    $ErrorMessage = $_.Exception.Message
    $ErrorTime = (Get-Date).ToString()
    LogWrite("")
    LogWrite("$ErrorTime : Error: Removing previous node $nl $ErrorMessage")
    Write-Output "There was an issue removing nodejs.  Chocolatey might be having issues, check to make sure node is uninstalled.  Also, the $Logfile will have more details."
    exit
}

# install nodejs
Write-Output "$nl----] Installing Node $nodeVersion [----$nl"
try
{
  Invoke-Call -ScriptBlock { choco install nodejs.install -y --version $nodeVersion }
}
catch
{
    $ErrorMessage = $_.Exception.Message
    $ErrorTime = (Get-Date).ToString()
    LogWrite("")
    LogWrite("$ErrorTime : Error: Installing node $nl $ErrorMessage")
    Write-Output "There was an issue installing nodejs.  Chocolatey might be having issues, check to make sure node is installed.  Also, the $Logfile will have more details."
    exit
}

# install npm
Write-Output "$nl----] Installing NPM $npmVersion [----$nl"
try
{
  Invoke-Call -ScriptBlock { npm install -g npm@$npmVersion }
}
catch
{
    $ErrorMessage = $_.Exception.Message
    $ErrorTime = (Get-Date).ToString()
    LogWrite("")
    LogWrite("$ErrorTime : Error: Installing npm $nl $ErrorMessage")
    Write-Output "There was an issue installing npm.  Also, the $Logfile will have more details."
    exit
}

# install angular cli
Write-Output "$nl----] Installing Angular CLI $angularVersion [----$nl"
try
{
  Invoke-Call -ScriptBlock { npm install -g @angular/cli@$angularVersion }
}
catch
{
    $ErrorMessage = $_.Exception.Message
    $ErrorTime = (Get-Date).ToString()
    LogWrite("")
    LogWrite("$ErrorTime : Error: Installing angular CLI $nl $ErrorMessage")
    Write-Output "There was an issue installing angular CLI.  Also, the $Logfile will have more details."
    exit
}

# install typescript cli
Write-Output "$nl----] Installing Typescript CLI $typescriptVersion [----$nl"
try
{
  Invoke-Call -ScriptBlock { npm install -g typescript@$typescriptVersion }
}
catch
{
    $ErrorMessage = $_.Exception.Message
    $ErrorTime = (Get-Date).ToString()
    LogWrite("")
    LogWrite("$ErrorTime : Error: Installing typescript $nl $ErrorMessage")
    Write-Output "There was an issue installing typescript.  Also, the $Logfile will have more details."
    exit
}

# install all dependencies
Write-Output "$nl----] Installing npm packages for this version [----$nl"
try
{
  Invoke-Call -ScriptBlock { npm install }
}
catch
{
    $ErrorMessage = $_.Exception.Message
    $ErrorTime = (Get-Date).ToString()
    LogWrite("")
    LogWrite("$ErrorTime : Error: npm install $nl $ErrorMessage")
    Write-Output "There was an issue with npm install.  Also, the $Logfile will have more details."
    exit
}

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
