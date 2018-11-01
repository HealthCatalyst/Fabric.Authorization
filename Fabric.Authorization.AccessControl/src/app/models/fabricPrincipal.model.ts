export interface IFabricPrincipal {
  subjectId: string;
  firstName?: string;
  middleName?: string;
  lastName?: string;
  principalType: string;
  identityProvider?: string;
  tenantId?: string;

  // Custom Property
  selected?: boolean;
}
