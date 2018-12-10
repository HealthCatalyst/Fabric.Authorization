export interface IFabricPrincipal {
  subjectId: string;
  firstName?: string;
  middleName?: string;
  lastName?: string;
  principalType: string;
  identityProvider?: string;
  tenantId?: string;
  externalIdentifier?: string;
  identityProviderUserPrincipalName?: string;

  // Custom Property
  selected?: boolean;
}
