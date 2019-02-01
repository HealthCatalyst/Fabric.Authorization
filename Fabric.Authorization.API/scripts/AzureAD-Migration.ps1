Import-Module -Name .\Move-ADObjectsToAzureAD-Utilities.psm1 -Force


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

function Get-AzureADGroupByName {
	param(
		[Parameter(Mandatory=$true)]
		[string] 
		[string] $groupName
	)

	Write-DosMessage -Level "Information" -Message "Retrieving group $($groupName) from Azure AD..."

	# connect to Azure AD

	$azureADGroups = Get-AzureADGroup -SearchString $groupName

	# disconnect from Azure AD
	Disconnect-AzureAD
}

function Move-ActiveDirectoryGroupsToAzureAD {
     param(
        [Parameter(Mandatory=$true)]
        [string] $connString
    )

    Write-DosMessage -Level "Information" -Message "Migrating AD groups to Azure AD..."

    # retrieve all groups from Auth DB    
    try {
        $results = Get-NonMigratedActiveDirectoryGroups -connectionString $connString
        foreach ($group in $results) {
            # query AD for SID
            # query Azure AD to get tenant ID and external ID


            # if SID from AD matches onprem ID from Azure AD, do update

            $tenantId = "tenant ID"
            $externalIdentifier = "external ID"

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
                Invoke-Sql $connString $sql @{groupId=$group.GroupId;tenantId=$tenantId;externalIdentifier=$externalIdentifier} | Out-Null
            }
            catch {
                Write-DosMessage -Level "Fatal" -Message "An error occurred while migrating AD groups to Azure AD. Connection String: $($connectionString). Error $($_.Exception)"
            }
        }
    }
    catch {
        Write-DosMessage -Level "Fatal" -Message "An error occurred while executing the command to retrieve non-migrated AD groups. Connection String: $($connString). Error $($_.Exception)"
    }
}