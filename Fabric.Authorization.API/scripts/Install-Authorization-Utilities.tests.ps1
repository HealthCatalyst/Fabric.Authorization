param(
    [string] $targetFilePath = "$PSScriptRoot\Install-Authorization-Utilities.psm1"
)

Write-Host $targetFilePath
Import-Module $targetFilePath -Force

Describe 'Get-FullyQualifiedMachineName Unit Tests' -Tag 'Unit' {
    Context 'Gets Machine Name' {
        InModuleScope Install-Authorization-Utilities {
            It 'Should return a non null machine name with at least three segments i.e. host.domain.local'{
                # Act
                $machineName = Get-FullyQualifiedMachineName
                $machineNameSegments = $machineName.Split(".")

                # Assert
                $machineName | Should -Not -BeNullOrEmpty
                $machineNameSegments.Length | Should -BeGreaterOrEqual 3
            }
        }
    }
}

Describe 'Get-Headers Unit Tests' -Tag 'Unit' {
    InModuleScope Install-Authorization-Utilities{
        Context 'With access token'{
            It 'Should Add access token to headers'{
                # Act
                $headers = Get-Headers -accessToken "myaccesstoken"

                # Assert
                $headers.Count | Should -Be 2
                $headers["Authorization"] | Should -Be "Bearer myaccesstoken"
                $headers["Accept"] | Should -Be "application/json"
            }
        }
        Context 'Without access token'{
            It 'Should add Accept header only when access token param is not passed'{
                #Act
                $headers = Get-Headers

                # Assert
                $headers.Count | Should -Be 1
                $headers["Accept"] | Should -Be "application/json"
            }
            It 'Should add Accept header only when access token param is null'{
                #Act
                $headers = Get-Headers $null

                # Assert
                $headers.Count | Should -Be 1
                $headers["Accept"] | Should -Be "application/json"
            }
            It 'Should add Accept header only when access token param is empty string'{
                #Act
                $headers = Get-Headers ""

                # Assert
                $headers.Count | Should -Be 1
                $headers["Accept"] | Should -Be "application/json"
            }
        }
    }    
}

Describe 'Get-Group tests' -Tag 'Unit'{
    InModuleScope Install-Authorization-Utilities{
        It 'Constructs proper GET url'{
            # Arrange
            Mock Invoke-Get {}

            # Act
            Get-Group -name "admins" -authorizationServiceUrl "https://host.domain.local/authorization" -accessToken "12345"

            # Assert
            Assert-MockCalled Invoke-Get -Times 1 -ParameterFilter {$url -eq "https://host.domain.local/authorization/groups/admins" -and $accessToken -eq "12345"}
        }
    }
}

Describe 'Get-Role tests' -Tag 'Unit'{
    InModuleScope Install-Authorization-Utilities{
        It 'Constructs proper GET url'{
            # Arrange
            Mock Invoke-Get {}

            # Act
            Get-Role -name "admin" -grain "app" -securableItem "testapp" -authorizationServiceUrl "https://host.domain.local/authorization" -accessToken "12345"

            # Assert
            Assert-MockCalled Invoke-Get -Times 1 -ParameterFilter {$url -eq "https://host.domain.local/authorization/roles/app/testapp/admin" -and $accessToken -eq "12345"}
        }
    }
}

Describe 'Add-Group tests' -Tag 'Unit'{
    InModuleScope Install-Authorization-Utilities{
        It 'Constructs proper POST'{
            # Arrange
            Mock Invoke-Post {}
            $postBody = @{
                groupName = "admins"
                groupSource = "Windows"
            }

            # Act
            Add-Group -name $postBody.groupName -source $postBody.groupSource -authUrl "https://host.domain.local/authorization" -accessToken "12345"

            # Assert
            Assert-MockCalled Invoke-Post -Times 1 -ParameterFilter {$url -eq "https://host.domain.local/authorization/groups" `
                                                                    -and $body.groupName -eq $postBody.groupName `
                                                                    -and $body.groupDisplayName -eq $postBody.groupDisplayName `
                                                                    -and $body.groupSource -eq $postBody.groupSource `
                                                                    -and $accessToken -eq "12345"}
        }
    }
}

Describe 'Add-User tests' -Tag 'Unit'{
    InModuleScope Install-Authorization-Utilities{
        It 'Constructs proper POST'{
            # Arrange
            Mock Invoke-Post {}
            $postBody = @{
                subjectId = "domain\test.user"
                identityProvider = "Windows"
            }

            # Act
            Add-User -name $postBody.subjectId -authUrl "https://host.domain.local/authorization" -accessToken "12345"

            # Assert
            Assert-MockCalled Invoke-Post -Times 1 -ParameterFilter {$url -eq "https://host.domain.local/authorization/user" `
                                                                    -and $body.subjectId -eq $postBody.subjectId `
                                                                    -and $body.identityProvider -eq $postBody.identityProvider `
                                                                    -and $accessToken -eq "12345"}
        }
    }
}

Describe 'Add-UserToGroup tests' -Tag 'Unit'{
    InModuleScope Install-Authorization-Utilities{
        It 'Constructs proper SQL parameters'{
            # Arrange
            Mock Invoke-Sql {}
            $group = @{
                Id = "test"
            }
            $user = @{
                identityProvider = "Windows";
                subjectId = "domain\test.user"
            }
            $connString = "localhost"
            $clientId = "testclient"

            # Act
            Add-UserToGroup -group $group -user $user -connString $connString -clientId $clientId

            # Assert
            Assert-MockCalled Invoke-Sql -Times 1 -ParameterFilter {$connectionString -eq $connString `
                                                                    -and $parameters.groupId -eq $group.Id `
                                                                    -and $parameters.identityProvider -eq $user.identityProvider `
                                                                    -and $parameters.subjectId -eq $user.subjectId `
                                                                    -and $parameters.clientId -eq $clientId}
        }
    }
}

Describe 'Add-ChildGroupToParentGroup tests' -Tag 'Unit'{
    InModuleScope Install-Authorization-Utilities{
        It 'Constructs proper SQL parameters'{
            # Arrange
            Mock Invoke-Sql {}
            $parentGroup = @{
                Id = "parent"
            }
            $childGroup = @{
                Id = "child"
            }
            $connString = "localhost"
            $clientId = "testclient"

            # Act
            Add-ChildGroupToParentGroup -parentGroup $parentGroup -childGroup $childGroup -connString $connString -clientId $clientId

            # Assert
            Assert-MockCalled Invoke-Sql -Times 1 -ParameterFilter {$connectionString -eq $connString `
                                                                    -and $parameters.parentGroupId -eq $parentGroup.Id `
                                                                    -and $parameters.childGroupId -eq $childGroup.Id `
                                                                    -and $parameters.clientId -eq $clientId}
        }
    }
}

Describe 'Add-RoleToGroup tests' -Tag 'Unit'{
    InModuleScope Install-Authorization-Utilities{
        It 'Constructs proper SQL parameters'{
            # Arrange
            Mock Invoke-Sql {}
            $role = @{
                Id = "testrole"
            }
            $group = @{
                Id = "testgroup"
            }
            $connString = "localhost"
            $clientId = "testclient"

            # Act
            Add-RoleToGroup -role $role -group $group -connString $connString -clientId $clientId

            # Assert
            Assert-MockCalled Invoke-Sql -Times 1 -ParameterFilter {$connectionString -eq $connString `
                                                                    -and $parameters.groupId -eq $group.Id `
                                                                    -and $parameters.roleId -eq $role.Id `
                                                                    -and $parameters.clientId -eq $clientId}
        }
    }
}

Describe 'Test-IsUser tests' -Tag 'Unit' {
    InModuleScope Install-Authorization-Utilities{
        It 'Should return true when the account is a valid user' {
            # Arrange
            Mock Get-PrincipalContext { "pc"}
            Mock Get-UserPrincipal { "not null value" }

            # Act
            $isUser = Test-IsUser -samAccountName "test.user" -domain "domain"

            # Assert
            $isUser | Should -Be $true
        }

        It 'Should return false the account is not a valid user' {
            # Arrange
            Mock Get-PrincipalContext { "pc" }
            Mock Get-UserPrincipal { $null }

            # Act
            $isUser = Test-IsUser -samAccountName "test.user" -domain "domain"

            # Assert
            $isUser | Should -Be $false
        }
    }
}

Describe 'Test-IsGroup tests' -Tag 'Unit' {
    InModuleScope Install-Authorization-Utilities{
        It 'Should return true when the account is a valid group' {
            # Arrange
            Mock Get-PrincipalContext { "pc"}
            Mock Get-GroupPrincipal { "not null value" }

            # Act
            $isGroup = Test-IsGroup -samAccountName "test.group" -domain "domain"

            # Assert
            $isGroup | Should -Be $true
        }

        It 'Should return false the account is not a valid group' {
            # Arrange
            Mock Get-PrincipalContext { "pc" }
            Mock Get-GroupPrincipal { $null }

            # Act
            $isGroup = Test-IsGroup -samAccountName "test.group" -domain "domain"

            # Assert
            $isGroup | Should -Be $false
        }
    }
}

Describe 'Get-SamAccountFromAccountName tests' -Tag 'Unit'{
    InModuleScope Install-Authorization-Utilities{
        It 'Should parse a valid account name'{
            #Arrange
            $accountName = "domain\test.user"

            #Act
            $samAccountName = Get-SamAccountFromAccountName -accountName $accountName

            #Assert
            $samAccountName | Should -Be "test.user"
        }
        It 'Should throw an exception for an invalid account name'{
            #Arrange
            $accountName = "domain\test\user"

            #Act
            {Get-SamAccountFromAccountName -accountName $accountName } |Should -Throw
        }
        It 'Should throw an exception for a null account name'{
            #Arrange
            $accountName = $null

            #Act
            {Get-SamAccountFromAccountName -accountName $accountName } |Should -Throw
        }
        It 'Should throw an exception for a empty account name'{
            #Arrange
            $accountName = ""

            #Act
            {Get-SamAccountFromAccountName -accountName $accountName } |Should -Throw
        }
    }
}

Describe 'Add-DosAdminGroup tests' -Tag 'Unit' {
    InModuleScope Install-Authorization-Utilities{
        Context 'Success'{
            It 'Should add the group'{
                # Arrange
                Mock Add-Group {}
                $testAuthUrl = "https://localhost/authorization"
                $testAccessToken = "12345"
                $testGroup = "testgroup"

                # Act
                Add-DosAdminGroup -authUrl $testAuthUrl -accessToken $testAccessToken -groupName $testGroup

                # Assert
                Assert-MockCalled Add-Group -Times 1 -ParameterFilter { $authUrl -eq $testAuthUrl `
                                                                        -and $name -eq $testGroup `
                                                                        -and $source -eq "custom" `
                                                                        -and $accessToken -eq $testAccessToken }
            }
            It 'Should get the group if the group exists'{
                #Arrange
                $testAuthUrl = "https://localhost/authorization"
                $testAccessToken = "12345"
                $testGroup = "testgroup"

                Mock Add-Group {throw "bad stuff that we can handle"}
                Mock Get-Group { return $testGroup }
                Mock Assert-WebExceptionType { $true }

                #Act
                $group = Add-DosAdminGroup -authUrl $testAuthUrl -accessToken $testAccessToken -groupName $testGroup

                # Assert
                $group | Should -Be $testGroup
                Assert-MockCalled Get-Group -Times 1 -ParameterFilter { $authorizationServiceUrl -eq $testAuthUrl `
                                                                        -and $name -eq $testGroup `
                                                                        -and $accessToken -eq $testAccessToken }
            }
        }
        Context 'Failure'{
            It 'Should throw an exception if we cannot add the group'{
                # Arrange
                Mock Add-Group { throw "bad error that we can't handle" }
                $testAuthUrl = "https://localhost/authorization"
                $testAccessToken = "12345"
                $testGroup = "testgroup"

                # Act
                {Add-DosAdminGroup -authUrl $testAuthUrl -accessToken $testAccessToken -groupName $testGroup} | Should -Throw "bad error that we can't handle"
            }
        }
    }
}

Describe 'Add-DosAdminRoleUsersToDosAdminGroup' -Tag 'Unit'{
    InModuleScope Install-Authorization-Utilities{
        It 'Constructs proper SQL parameters'{
            # Arrange
            Mock Invoke-Sql {}
            $groupId = [Guid]::NewGuid()
            $connString = "localhost"
            $clientId = "testclient"
            $roleName = "testrole"
            $securableName = "testapp"

            # Act
            Add-DosAdminRoleUsersToDosAdminGroup -groupId $groupId -connectionString $connString -clientId $clientId -roleName $roleName -securableName $securableName

            # Assert
            Assert-MockCalled Invoke-Sql -Times 1 -ParameterFilter {$connectionString -eq $connString `
                                                                    -and $parameters.dosAdminGroupId -eq $groupId `
                                                                    -and $parameters.clientId -eq $clientId `
                                                                    -and $parameters.roleName -eq $roleName `
                                                                    -and $parameters.securableName -eq $securableName}
        }
        It 'throws an exception if the sql statment fails'{
            # Arrange
            Mock Invoke-Sql {throw "bad stuff happened"}
            $groupId = [Guid]::NewGuid()
            $connString = "localhost"
            $clientId = "testclient"
            $roleName = "testrole"
            $securableName = "testapp"

            #Act
            {Add-DosAdminRoleUsersToDosAdminGroup -groupId $groupId -connectionString $connString -clientId $clientId -roleName $roleName -securableName $securableName} | Should -Throw "bad stuff happened"
        }
    }
}

Describe 'Remove-UsersFromDosAdminRole' -Tag 'Unit'{
    InModuleScope Install-Authorization-Utilities{
        It 'Constructs proper SQL parameters'{
            # Arrange
            Mock Invoke-Sql {}
            $connString = "localhost"
            $clientId = "testclient"
            $roleName = "testrole"
            $securableName = "testapp"

            # Act
            Remove-UsersFromDosAdminRole -connectionString $connString -clientId $clientId -roleName $roleName -securableName $securableName

            # Assert
            Assert-MockCalled Invoke-Sql -Times 1 -ParameterFilter {$connectionString -eq $connString `
                                                                    -and $parameters.clientId -eq $clientId `
                                                                    -and $parameters.roleName -eq $roleName `
                                                                    -and $parameters.securableName -eq $securableName}
        }
        It 'throws an exception if the sql statment fails'{
            # Arrange
            Mock Invoke-Sql {throw "bad stuff happened"}
            $connString = "localhost"
            $clientId = "testclient"
            $roleName = "testrole"
            $securableName = "testapp"

            #Act
            {Remove-UsersFromDosAdminRole -connectionString $connString -clientId $clientId -roleName $roleName -securableName $securableName} | Should -Throw "bad stuff happened"
        }
    }
}

Describe 'Add-DosAdminGroupRolesToDosAdminChildGroups' -Tag 'Unit'{
    InModuleScope Install-Authorization-Utilities{
        It 'Constructs proper SQL parameters'{
            # Arrange
            Mock Invoke-Sql {}
            $groupId = [Guid]::NewGuid()
            $connString = "localhost"
            $clientId = "testclient"
            $roleName = "testrole"
            $securableName = "testapp"

            # Act
            Add-DosAdminGroupRolesToDosAdminChildGroups -groupId $groupId -connectionString $connString -clientId $clientId -roleName $roleName -securableName $securableName

            # Assert
            Assert-MockCalled Invoke-Sql -Times 1 -ParameterFilter {$connectionString -eq $connString `
                                                                    -and $parameters.dosAdminGroupId -eq $groupId `
                                                                    -and $parameters.clientId -eq $clientId `
                                                                    -and $parameters.roleName -eq $roleName `
                                                                    -and $parameters.securableName -eq $securableName}
        }
        It 'throws an exception if the sql statment fails'{
            # Arrange
            Mock Invoke-Sql {throw "bad stuff happened"}
            $groupId = [Guid]::NewGuid()
            $connString = "localhost"
            $clientId = "testclient"
            $roleName = "testrole"
            $securableName = "testapp"

            #Act
            {Add-DosAdminGroupRolesToDosAdminChildGroups -groupId $groupId -connectionString $connString -clientId $clientId -roleName $roleName -securableName $securableName} | Should -Throw "bad stuff happened"
        }
    }
}

Describe 'Remove-GroupsFromDosAdminRole' -Tag 'Unit'{
    InModuleScope Install-Authorization-Utilities{
        It 'Constructs proper SQL parameters'{
            # Arrange
            Mock Invoke-Sql {}
            $connString = "localhost"
            $clientId = "testclient"
            $roleName = "testrole"
            $securableName = "testapp"

            # Act
            Remove-GroupsFromDosAdminRole -connectionString $connString -clientId $clientId -roleName $roleName -securableName $securableName

            # Assert
            Assert-MockCalled Invoke-Sql -Times 1 -ParameterFilter {$connectionString -eq $connString `
                                                                    -and $parameters.clientId -eq $clientId `
                                                                    -and $parameters.roleName -eq $roleName `
                                                                    -and $parameters.securableName -eq $securableName}
        }
        It 'throws an exception if the sql statment fails'{
            # Arrange
            Mock Invoke-Sql {throw "bad stuff happened"}
            $connString = "localhost"
            $clientId = "testclient"
            $roleName = "testrole"
            $securableName = "testapp"

            #Act
            {Remove-GroupsFromDosAdminRole -connectionString $connString -clientId $clientId -roleName $roleName -securableName $securableName} | Should -Throw "bad stuff happened"
        }
    }
}

Describe 'Add-DosAdminRoleToDosAdminGroup' -Tag 'Unit'{
    InModuleScope Install-Authorization-Utilities{
        It 'Constructs proper SQL parameters'{
            # Arrange
            Mock Invoke-Sql {}
            $groupId = [Guid]::NewGuid()
            $connString = "localhost"
            $clientId = "testclient"
            $roleName = "testrole"
            $securableName = "testapp"

            # Act
            Add-DosAdminRoleToDosAdminGroup -groupId $groupId -connectionString $connString -clientId $clientId -roleName $roleName -securableName $securableName

            # Assert
            Assert-MockCalled Invoke-Sql -Times 1 -ParameterFilter {$connectionString -eq $connString `
                                                                    -and $parameters.dosAdminGroupId -eq $groupId `
                                                                    -and $parameters.clientId -eq $clientId `
                                                                    -and $parameters.roleName -eq $roleName `
                                                                    -and $parameters.securableName -eq $securableName}
        }
        It 'throws an exception if the sql statment fails'{
            # Arrange
            Mock Invoke-Sql {throw "bad stuff happened"}
            $groupId = [Guid]::NewGuid()
            $connString = "localhost"
            $clientId = "testclient"
            $roleName = "testrole"
            $securableName = "testapp"

            #Act
            {Add-DosAdminRoleToDosAdminGroup -groupId $groupId -connectionString $connString -clientId $clientId -roleName $roleName -securableName $securableName} | Should -Throw "bad stuff happened"
        }
    }
}

Describe 'Update-DosAdminRoleToDataMartAdmin' -Tag 'Unit'{
    InModuleScope Install-Authorization-Utilities{
        It 'Constructs proper SQL parameters'{
            # Arrange
            Mock Invoke-Sql {}
            $connString = "localhost"
            $clientId = "testclient"
            $oldRoleName = "testrole"
            $newRoleName = "testrole2"
            $securableName = "testapp"

            # Act
            Update-DosAdminRoleToDataMartAdmin -connectionString $connString -clientId $clientId -oldRoleName $oldRoleName -newRoleName $newRoleName -securableName $securableName

            # Assert
            Assert-MockCalled Invoke-Sql -Times 1 -ParameterFilter {$connectionString -eq $connString `
                                                                    -and $parameters.clientId -eq $clientId `
                                                                    -and $parameters.oldRoleName -eq $oldRoleName `
                                                                    -and $parameters.newRoleName -eq $newRoleName `
                                                                    -and $parameters.securableName -eq $securableName}
        }
        It 'throws an exception if the sql statment fails'{
            # Arrange
            Mock Invoke-Sql {throw "bad stuff happened"}
            $connString = "localhost"
            $clientId = "testclient"
            $oldRoleName = "testrole"
            $newRoleName = "testrole2"
            $securableName = "testapp"

            #Act
            {Update-DosAdminRoleToDataMartAdmin -connectionString $connString -clientId $clientId -oldRoleName $oldRoleName -newRoleName $newRoleName -securableName $securableName} | Should -Throw "bad stuff happened"
        }
    }
}

Describe 'Remove-DosAdminRole' -Tag 'Unit'{
    InModuleScope Install-Authorization-Utilities{
        It 'Constructs proper SQL parameters'{
            # Arrange
            Mock Invoke-Sql {}
            $connString = "localhost"
            $clientId = "testclient"
            $roleName = "testrole"
            $securableName = "testapp"

            # Act
            Remove-DosAdminRole -connectionString $connString -clientId $clientId -roleName $roleName -securableName $securableName

            # Assert
            Assert-MockCalled Invoke-Sql -Times 1 -ParameterFilter {$connectionString -eq $connString `
                                                                    -and $parameters.clientId -eq $clientId `
                                                                    -and $parameters.roleName -eq $roleName `
                                                                    -and $parameters.securableName -eq $securableName}
        }
        It 'throws an exception if the sql statment fails'{
            # Arrange
            Mock Invoke-Sql {throw "bad stuff happened"}
            $connString = "localhost"
            $clientId = "testclient"
            $roleName = "testrole"
            $securableName = "testapp"

            #Act
            {Remove-DosAdminRole -connectionString $connString -clientId $clientId -roleName $roleName -securableName $securableName} | Should -Throw "bad stuff happened"
        }
    }
}

Describe 'Test-FabricRegistrationStepAlreadyComplete' -Tag 'Unit'{
    InModuleScope Install-Authorization-Utilities{
        It 'Returns true when dosadmin Role and DataMartAdmin Role exist'{
            # Arrange
            Mock Get-Role -ParameterFilter {$name -eq "DataMartAdmin"} { return "DataMartAdmin" }
            Mock Get-Role -ParameterFilter {$name -eq "dosadmin"} { return "dosadmin" }

            # Act
            $result = Test-FabricRegistrationStepAlreadyComplete -authUrl "http://host.domain.local/authorization" -accessToken "12345"

            # Assert
            $result | Should -Be $true
        }
        It 'Returns false when dosadmin Role does not exist'{
            # Arrange
            Mock Get-Role -ParameterFilter {$name -eq "DataMartAdmin"} { return "DataMartAdmin" }
            Mock Get-Role -ParameterFilter {$name -eq "dosadmin"} { return $null }

            # Act
            $result = Test-FabricRegistrationStepAlreadyComplete -authUrl "http://host.domain.local/authorization" -accessToken "12345"

            # Assert
            $result | Should -Be $false
        }
        It 'Returns false when DataMartAdmin role does not exist'{
            # Arrange
            Mock Get-Role -ParameterFilter {$name -eq "DataMartAdmin"} { return $null }
            Mock Get-Role -ParameterFilter {$name -eq "dosadmin"} { return "dosadmin" }

            # Act
            $result = Test-FabricRegistrationStepAlreadyComplete -authUrl "http://host.domain.local/authorization" -accessToken "12345"

            # Assert
            $result | Should -Be $false
        }
        It 'Returns false when DataMartAdmin and dosadmin roles do not exist'{
            # Arrange
            Mock Get-Role -ParameterFilter {$name -eq "DataMartAdmin"} { return $null }
            Mock Get-Role -ParameterFilter {$name -eq "dosadmin"} { return $null }

            # Act
            $result = Test-FabricRegistrationStepAlreadyComplete -authUrl "http://host.domain.local/authorization" -accessToken "12345"

            # Assert
            $result | Should -Be $false
        }
        It 'Throws exception when Get-Role throws an exception'{
            # Arrange
            Mock Get-Role -ParameterFilter {$name -eq "DataMartAdmin"} { return $null }
            Mock Get-Role -ParameterFilter {$name -eq "dosadmin"} { throw "bad stuff happened" }

            # Act
            {Test-FabricRegistrationStepAlreadyComplete -authUrl "http://host.domain.local/authorization" -accessToken "12345" } | Should -Throw
        }
    }
}

Describe 'Get-CurrentUserDomain tests' -Tag 'Unit'{
    InModuleScope Install-Authorization-Utilities{
        Context 'Quiet mode'{
            It 'Should not prompt for domain in quiet mode'{
                # Arrange
                $expectedCurrentDomain = $env:USERDNSDOMAIN
                Mock Read-Host {}

                # Act
                $currentDomain = Get-CurrentUserDomain -quiet $true

                # Assert
                Assert-MockCalled Read-Host -Times 0
                $currentDomain | Should -Be $expectedCurrentDomain
            }
        }
        Context 'Interactive mode'{
            It 'Should prompt for domain in interactive mode and return user entered domain'{
                # Arrange
                $expectedCurrentDomain = "domain.local"
                Mock Read-Host { $expectedCurrentDomain }

                # Act
                $currentDomain = Get-CurrentUserDomain -quiet $false

                # Assert
                Assert-MockCalled Read-Host -Times 1
                $currentDomain | Should -Be $expectedCurrentDomain
            }
            It 'Should prompt for domain in interactive mode and return default domain'{
                # Arrange
                $expectedCurrentDomain = $env:USERDNSDOMAIN
                Mock Read-Host { "" }

                # Act
                $currentDomain = Get-CurrentUserDomain -quiet $false

                # Assert
                Assert-MockCalled Read-Host -Times 1
                $currentDomain | Should -Be $expectedCurrentDomain
            }
        }
    }
}

Describe 'Get-DefaultIdentityServiceUrl' -Tag 'Unit'{
    InModuleScope Install-Authorization-Utilities{
        It 'Should return the passed in URL'{
            # Arrange
            $expectedIdentityUrl = "http://host.domain.local/identity"

            # Act
            $identityUrl = Get-DefaultIdentityServiceUrl -identityServiceUrl $expectedIdentityUrl

            # Assert
            $identityUrl | Should -Be $expectedIdentityUrl
        }
        It 'Should return the constructed URL'{
            # Arrange
            $expectedMachineHostName = "http://host.domain.local"
            $expectedIdentityUrl = "$expectedMachineHostName/identity"
            Mock Get-FullyQualifiedMachineName { return $expectedMachineHostName}

            # Act
            $identityUrl = Get-DefaultIdentityServiceUrl -identityServiceUrl $null

            # Assert
            $identityUrl | Should -Be $expectedIdentityUrl
        }
    }
}

Describe 'Install-UrlRewriteIfNeeded' -Tag 'Unit'{
    InModuleScope Install-Authorization-Utilities{
        Context 'url rewrite does not exist'{
            It 'Installs url rewrite if minimum version is not present'{
                # Arrange
                Mock Test-Prerequisite { return $false}
                Mock Invoke-WebRequest {}
                Mock Start-Process {}
                Mock Remove-Item {}

                # Act
                Install-UrlRewriteIfNeeded -version "1.1.1.1" -downloadUrl "http://host.domain.local/url-rewrite.msi"

                # Assert
                Assert-MockCalled Invoke-WebRequest -Times 1
                Assert-MockCalled Start-Process -Times 1
            }
            It 'Throws an exception when Start-Process fails'{
                # Arrange
                Mock Test-Prerequisite { return $false}
                Mock Invoke-WebRequest {}
                Mock Start-Process {throw "bad stuff happened"}
                Mock Remove-Item {}

                # Act
                {Install-UrlRewriteIfNeeded -version "1.1.1.1" -downloadUrl "http://host.domain.local/url-rewrite.msi"} | Should -Throw
            }
            It 'Should warn when we cannot clean up the install files, but not throw an error'{
                # Arrange
                Mock Test-Prerequisite { return $false}
                Mock Invoke-WebRequest {}
                Mock Start-Process {}
                Mock Remove-Item { throw "bad stuff happened"}
                Mock Write-DosMessage {}

                # Act
                {Install-UrlRewriteIfNeeded -version "1.1.1.1" -downloadUrl "http://host.domain.local/url-rewrite.msi"} | Should -Not -Throw

                # Assert
                Assert-MockCalled Write-DosMessage -ParameterFilter {$Level -eq "Warning"} -Times 2
            }
        }
        Context 'Url rewrite exists'{
            It 'Does not install url rewrite if minimum version is present'{
                # Arrange
                Mock Test-Prerequisite { return $true}
                Mock Invoke-WebRequest {}
                Mock Start-Process {}
                Mock Write-DosMessage {}

                # Act
                Install-UrlRewriteIfNeeded -version "1.1.1.1" -downloadUrl "http://host.domain.local/url-rewrite.msi"

                # Assert
                Assert-MockCalled Invoke-WebRequest -Times 0
                Assert-MockCalled Start-Process -Times 0
                Assert-MockCalled Write-DosMessage -Times 1
            }
        }
    }
}

Describe 'Get-AuthorizationDatabaseConnectionString Tests' -Tag 'Unit'{
    InModuleScope Install-Authorization-Utilities{
        Context 'Quiet Mode'{
            It 'Should not prompt for the database name'{
                # Arrange
                Mock Invoke-Sql {}
                Mock Add-InstallationSetting {}
                Mock Read-Host {}
                $authorizationDbName = "Authorization"
                $sqlServerAddress = "localhost"
                $expectedSqlServerAddress = "Server=$($sqlServerAddress);Database=$($authorizationDbName);Trusted_Connection=True;MultipleActiveResultSets=True;"

                # Act
                $authorizationDatabase = Get-AuthorizationDatabaseConnectionString -authorizationDbName $authorizationDbName -sqlServerAddress $sqlServerAddress -installConfigPath "install.config" -quiet $true

                # Assert
                $authorizationDatabase.DbConnectionString | Should -Be $expectedSqlServerAddress
                $authorizationDatabase.DbName | Should -Be $authorizationDbName
                Assert-MockCalled Invoke-Sql -Times 1
                Assert-MockCalled Add-InstallationSetting -Times 1
                Assert-MockCalled Read-Host -Times 0
            }
        }
        Context 'Interactive Mode'{
            It 'Should prompt for database name and use entered value'{
                # Arrange
                Mock Invoke-Sql {}
                Mock Add-InstallationSetting {}
                $storedAuthorizationDbName = "Authorization"
                $userEnteredAuthorizationDbName = "Authorization2"
                $sqlServerAddress = "localhost"
                $expectedSqlServerAddress = "Server=$($sqlServerAddress);Database=$($userEnteredAuthorizationDbName);Trusted_Connection=True;MultipleActiveResultSets=True;"
                Mock Read-Host { return $userEnteredAuthorizationDbName }

                # Act
                $authorizationDatabase = Get-AuthorizationDatabaseConnectionString -authorizationDbName $storedAuthorizationDbName -sqlServerAddress $sqlServerAddress -installConfigPath "install.config" -quiet $false

                # Assert
                $authorizationDatabase.DbConnectionString | Should -Be $expectedSqlServerAddress
                $authorizationDatabase.DbName | Should -Be $userEnteredAuthorizationDbName
                Assert-MockCalled Invoke-Sql -Times 1
                Assert-MockCalled Add-InstallationSetting -Times 1
                Assert-MockCalled Read-Host -Times 1
            }
            It 'Should prompt for database name and use default value if no value is entered'{
                # Arrange
                Mock Invoke-Sql {}
                Mock Add-InstallationSetting {}
                $storedAuthorizationDbName = "Authorization"
                $userEnteredAuthorizationDbName = ""
                $sqlServerAddress = "localhost"
                $expectedSqlServerAddress = "Server=$($sqlServerAddress);Database=$($storedAuthorizationDbName);Trusted_Connection=True;MultipleActiveResultSets=True;"
                Mock Read-Host { return $userEnteredAuthorizationDbName }

                # Act
                $authorizationDatabase = Get-AuthorizationDatabaseConnectionString -authorizationDbName $storedAuthorizationDbName -sqlServerAddress $sqlServerAddress -installConfigPath "install.config" -quiet $false

                # Assert
                $authorizationDatabase.DbConnectionString | Should -Be $expectedSqlServerAddress
                $authorizationDatabase.DbName | Should -Be $storedAuthorizationDbName
                Assert-MockCalled Invoke-Sql -Times 1
                Assert-MockCalled Add-InstallationSetting -Times 1
                Assert-MockCalled Read-Host -Times 1
            }
        }
    }
}

Describe 'Get-IdentityServiceUrl tests' -Tag 'Unit'{
    InModuleScope Install-Authorization-Utilities{
        Context 'Quiet mode with no default'{
            It 'Should not prompt for Identity URL, and returns default URL when none is provided'{
                # Arrange
                $defaultIdentityServiceUrl = "https://host.domain.local/identity"
                Mock Get-DefaultIdentityServiceUrl { $defaultIdentityServiceUrl }
                Mock Add-InstallationSetting {}
                Mock Read-Host {}

                # Act
                $identityServiceUrl = Get-IdentityServiceUrl -identityServiceUrl "" -installConfigPath "install.config" -quiet $true

                # Assert
                $identityServiceUrl | Should -Be $defaultIdentityServiceUrl
                Assert-MockCalled Add-InstallationSetting -Times 1
                Assert-MockCalled Read-Host -Times 0
            }
        }
        Context 'Quiet mode with default'{
            It 'Should not prompt for Identity URL, and returns stored URL when passed in'{
                # Arrange
                $storedIdentityServiceUrl = "https://host.domain.local/identity2"
                Mock Add-InstallationSetting {}
                Mock Read-Host {}

                # Act
                $identityServiceUrl = Get-IdentityServiceUrl -identityServiceUrl $storedIdentityServiceUrl -installConfigPath "install.config" -quiet $true

                # Assert
                $identityServiceUrl | Should -Be $storedIdentityServiceUrl
                Assert-MockCalled Add-InstallationSetting -Times 1
                Assert-MockCalled Read-Host -Times 0
            }
        }
        Context 'Interactive mode with no default'{
            It 'Should prompt for Identity URL, and return user entered URL'{
                # Arrange
                $defaultIdentityServiceUrl = "https://host.domain.local/identity"
                $userEnteredIdentityServiceUrl = "https://host.domain.local/identity2"
                Mock Get-DefaultIdentityServiceUrl { $defaultIdentityServiceUrl }
                Mock Add-InstallationSetting {}
                Mock Read-Host { return $userEnteredIdentityServiceUrl }

                # Act
                $identityServiceUrl = Get-IdentityServiceUrl -identityServiceUrl "" -installConfigPath "install.config" -quiet $false

                # Assert
                $identityServiceUrl | Should -Be $userEnteredIdentityServiceUrl
                Assert-MockCalled Add-InstallationSetting -Times 1
                Assert-MockCalled Read-Host -Times 1
            }
        }
        Context 'Interactive mode with no default'{
            It 'Should prompt for Identity URL, and return deafult URL when user does not enter a url'{
                # Arrange
                $defaultIdentityServiceUrl = "https://host.domain.local/identity"
                $userEnteredIdentityServiceUrl = ""
                Mock Get-DefaultIdentityServiceUrl { $defaultIdentityServiceUrl }
                Mock Add-InstallationSetting {}
                Mock Read-Host { return $userEnteredIdentityServiceUrl }

                # Act
                $identityServiceUrl = Get-IdentityServiceUrl -identityServiceUrl "" -installConfigPath "install.config" -quiet $false

                # Assert
                $identityServiceUrl | Should -Be $defaultIdentityServiceUrl
                Assert-MockCalled Add-InstallationSetting -Times 1
                Assert-MockCalled Read-Host -Times 1
            }
        }
    }
}
Describe 'Register-AuthorizationWithDiscovery tests' -Tag 'Unit'{
    InModuleScope Install-Authorization-Utilities{
        It 'Should register with Discovery'{
            # Arrange
            Mock Add-ServiceUserToDiscovery {}
            Mock Add-DiscoveryRegistrationSql {}
            $iisUserName = "domain\service.user"
            $metadataConnStr = "localhost"

            # Act
            Register-AuthorizationWithDiscovery -iisUserName $iisUserName -metadataConnStr $metadataConnStr -version "1.1.1.1" -authorizationServiceUrl "https://host.domain.local/authorization"

            # Assert
            Assert-MockCalled Add-ServiceUserToDiscovery -ParameterFilter { $userName -eq $iisUserName -and $connString -eq $metadataConnStr } -Times 1
            Assert-MockCalled Add-DiscoveryRegistrationSql -Times 1 -ParameterFilter {$discoveryPostBody.buildVersion -eq "1.1.1.1" -and `
                                                                                      $discoveryPostBody.serviceName -eq "AuthorizationService" -and `
                                                                                      $discoveryPostBody.serviceVersion -eq 1 -and `
                                                                                      $discoveryPostBody.friendlyName -eq "Fabric.Authorization" -and `
                                                                                      $discoveryPostBody.description -eq "The Fabric.Authorization service provides centralized authentication across the Fabric ecosystem." -and `
                                                                                      $discoveryPostBody.serviceUrl -eq "https://host.domain.local/authorization/v1" -and `
                                                                                      $discoveryPostBody.discoveryType -eq "Service"}
        }
    }
}

Describe 'Register-AccessControlWithDiscovery tests' -Tag 'Unit'{
    InModuleScope Install-Authorization-Utilities{
        It 'Should register with Discovery'{
            # Arrange
            Mock Add-ServiceUserToDiscovery {}
            Mock Add-DiscoveryRegistrationSql {}
            $iisUserName = "domain\service.user"
            $metadataConnStr = "localhost"

            # Act
            Register-AccessControlWithDiscovery -iisUserName $iisUserName -metadataConnStr $metadataConnStr -version "1.1.1.1" -authorizationServiceUrl "https://host.domain.local/authorization"

            # Assert
            Assert-MockCalled Add-ServiceUserToDiscovery -ParameterFilter { $userName -eq $iisUserName -and $connString -eq $metadataConnStr } -Times 1
            Assert-MockCalled Add-DiscoveryRegistrationSql -Times 1 -ParameterFilter {$discoveryPostBody.buildVersion -eq "1.1.1.1" -and `
                                                                                      $discoveryPostBody.serviceName -eq "AccessControl" -and `
                                                                                      $discoveryPostBody.serviceVersion -eq 1 -and `
                                                                                      $discoveryPostBody.friendlyName -eq "Access Control" -and `
                                                                                      $discoveryPostBody.description -eq "Fabric Access Control provides a UI to manage permissions across DOS." -and `
                                                                                      $discoveryPostBody.serviceUrl -eq "https://host.domain.local/authorization" -and `
                                                                                      $discoveryPostBody.discoveryType -eq "Application" -and `
                                                                                      $discoveryPostBody.isHidden -eq $false -and `
                                                                                      $discoveryPostBody.iconTxt -ne $null}
        }
    }
}
