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
