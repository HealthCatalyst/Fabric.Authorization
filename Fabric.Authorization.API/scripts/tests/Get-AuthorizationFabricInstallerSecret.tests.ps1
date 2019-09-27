param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-Authorization-Utilities.psm1"
)

 Write-Host $targetFilePath
# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

 Describe 'Get-AuthorizationFabricInstallerSecret' {
    InModuleScope Install-Authorization-Utilities {
        Context 'Happy Paths' {
            BeforeAll {
                $encryptionCertificateThumbprint = "some thumbprint"
            }

            It 'Should decrypt and return the installer secret' {
                # Arrange
                $fabricInstallerSecret = "!!enc!!:some secret"
                $returnSecret = "some secret"
                Mock -CommandName  Unprotect-DosInstallerSecret { return $returnSecret }

                # Act
                $decryptedSecret = Get-AuthorizationFabricInstallerSecret `
                    -fabricInstallerSecret $fabricInstallerSecret `
                    -encryptionCertificateThumbprint $encryptionCertificateThumbprint

                # Assert
                $decryptedSecret | Should -eq $returnSecret
            }

            It 'Should not decrypt the installer secret when not encrypted' {
                # Arrange
                $fabricInstallerSecret = "some secret"
                Mock -CommandName  Unprotect-DosInstallerSecret { return $fabricInstallerSecret }

                # Act
                $decryptedSecret = Get-AuthorizationFabricInstallerSecret `
                    -fabricInstallerSecret $fabricInstallerSecret `
                    -encryptionCertificateThumbprint $encryptionCertificateThumbprint

                # Assert
                $decryptedSecret | Should -eq $fabricInstallerSecret
                Assert-MockCalled Unprotect-DosInstallerSecret -Times 0 -Scope 'It'
            }
        }
        Context 'Unhappy Paths' {
            BeforeAll {
                $encryptionCertificateThumbprint = "some thumbprint"
            }

            It 'Should create an error log when no secret is returned' {
                # Arrange
                $fabricInstallerSecret = "!!enc!!:some secret"
                $newSecret = "new secret"
                Mock -CommandName  Unprotect-DosInstallerSecret { } # Simulates error occured
                Mock -CommandName Invoke-ResetFabricInstallerSecret { return $newSecret }
                Mock -CommandName Write-DosMessage { throw }

                # Act
                { $decryptedSecret = Get-AuthorizationFabricInstallerSecret `
                    -fabricInstallerSecret $fabricInstallerSecret `
                    -encryptionCertificateThumbprint $encryptionCertificateThumbprint 
                } | Should -Throw

                # Assert
                Assert-MockCalled Write-DosMessage -Times 1 -Scope 'It' -ParameterFilter {
                    $Level -eq "Error" `
                    -and $Message.StartsWith("There was an error decrypting the installer secret.")
                }
            }
        }
    }
}