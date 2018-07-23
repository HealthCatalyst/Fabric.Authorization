export interface IFabricPrincipal {
  subjectId: string;
  firstName?: string;
  middleName?: string;
  lastName?: string;
  principalType: string;

  // Custom Property
  selected?: boolean;
}
