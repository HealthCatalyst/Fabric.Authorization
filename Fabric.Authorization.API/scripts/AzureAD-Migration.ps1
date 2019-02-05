#Requires -RunAsAdministrator
#Requires -Version 5.1
#Requires -Modules PowerShellGet, PackageManagement

param(
    [PSCredential] $credential,
    [ValidateScript({
        if (!(Test-Path $_)) {
            throw "Path $_ does not exist. Please enter valid path to the install.config."
        }
        if (!(Test-Path $_ -PathType Leaf)) {
            throw "Path $_ is not a file. Please enter a valid path to the install.config."
        }
        return $true
    })] 
    [string] $installConfigPath = "$PSScriptRoot\install.config",
    [switch] $quiet
)

Import-Module -Name .\AzureAD-Utilities.psm1 -Force
Import-Module ActiveDirectory

$authorizationDatabase = Get-AuthorizationDatabaseConnectionString -authorizationDbName $installSettings.authorizationDbName -sqlServerAddress $sqlServerAddress -installConfigPath $installConfigPath -quiet $quiet
Move-ActiveDirectoryGroupsToAzureAD -connString "$($authorizationDatabase.DbConnectionString)"

function Get-NonMigratedActiveDirectoryGroups {
    param(
        [Parameter(Mandatory=$true)]
        [string] $connectionString
    )

    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)	
    $sql = "SELECT GroupId, Name, IdentityProvider, ExternalIdentifier, TenantId
              FROM Groups
              WHERE IsDeleted = 0
              AND IdentityProvider = 'Windows'
              AND Source = 'Directory'"
    $command = New-Object System.Data.SqlClient.SqlCommand($sql, $connection)
    
    Write-DosMessage -Level "Information" -Message "Retrieving groups for migration..."

    $groups = @();
    try {	
        $connection.Open()
        $adapter = New-Object System.Data.sqlclient.sqlDataAdapter $command
        $groups = New-Object System.Data.DataTable
        $adapter.Fill($groups) | Out-Null
        $connection.Close()
    }
    catch [System.Data.SqlClient.SqlException] {
        Write-DosMessage -Level "Fatal" -Message "An error occurred while executing the command to retrieve non-migrated AD groups. Connection String: $($connectionString). Error $($_.Exception)"
    }    	
    
    return $groups;
}

function Get-AzureADGroupBySID {
    param(
        [Parameter(Mandatory=$true)]
        [string] $tenantId,
        [Parameter(Mandatory=$true)]
        [string] $groupSID
    )

    $azureADGroups = @()
    try {
        # connect to Azure AD
        Connect-AzureADTenant -tenantId $tenantId

        Write-DosMessage -Level "Information" -Message "Retrieving group $($groupName) from Azure AD..."
        $azureADGroups = Get-AzureADGroup -Filter "onPremisesSecurityIdentifier eq '$($groupSID)'"

        # disconnect from Azure AD
        Disconnect-AzureAD
    }
    catch {
        Write-DosMessage -Level "Fatal" -Message "Error while attempting to retrieve Azure AD group $($groupSID): $($_.Exception)"
    }

    if ($null -ne $azureADGroups.Count -eq 1) {
        return $azureADGroups[0]
    }
    else {
        $errorMsg = "Multiple matches found for group SID $($groupSID)."
        Write-DosMessage -Level "Error" -Message $errorMsg
        throw $errorMsg
    }
}

function Move-ActiveDirectoryGroupsToAzureAD {
     param(
        [Parameter(Mandatory=$true)]
        [string] $connString
    )

    Write-DosMessage -Level "Information" -Message "Migrating AD groups to Azure AD..."

    $allowedTenantIds = Get-Tenants -installConfigPath $installConfigPath

    # retrieve all groups from Auth DB    
    try {
        $results = Get-NonMigratedActiveDirectoryGroups -connectionString $connString
    }
    catch {
        Write-DosMessage -Level "Fatal" -Message "An error occurred while executing the command to retrieve non-migrated AD groups. Connection String: $($connString). Error $($_.Exception)"
    }
    foreach ($group in $results) {
        # query AD for SID
        $adGroupSID = ""
        try {
            $adGroup = Get-ADGroup -Identity "$($group.Name)"
            $adGroupSID = $adGroup.SID.Value
        }
        catch {
            Write-DosMessage -Level "Fatal" -Message "An error occurred while retrieving AD Group $($group.Name) from Active Directory. Error $($_.Exception)"
            continue
        }

        foreach ($tenantId in $allowedTenantIds) {
            # query Azure AD to get external ID
            $azureADGroup = Get-AzureADGroupBySID -tenantId $tenantId -groupSID $adGroupSID

            # if group exists, then it's a match so migrate
            if ($null -ne $azureADGroup) {
                $sql = "UPDATE g 
                SET g.IdentityProvider = 'AzureActiveDirectory',
                    g.TenantId = @tenantId,
                    g.ExternalIdentifier = @externalIdentifier,
                    g.ModifiedBy = 'fabric-installer',
                    g.ModifiedDateTimeUtc = GETUTCDATE()
                FROM Groups g
                WHERE g.[GroupId] = @groupId;"
    
                Write-DosMessage -Level "Information" -Message "Migrating group $($group.Name) to Azure AD..."
                try {
                    Invoke-Sql $connString $sql @{groupId=$group.GroupId;tenantId=$tenantId;externalIdentifier=$($azureADGroup.ObjectId)} | Out-Null
                }
                catch {
                    Write-DosMessage -Level "Fatal" -Message "An error occurred while migrating AD groups to Azure AD. Connection String: $($connectionString). Error $($_.Exception)"
                }

                break
            }
        }
    }
}