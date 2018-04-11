export interface IRole {
  id?: string;
  parentRole?: string;
  childRoles?: Array<string>;
  name: string;
  grain: string;
  securableItem: string;
  displayName?: string;
  description?: string;

  // Custom property
  selected?: boolean;
}
