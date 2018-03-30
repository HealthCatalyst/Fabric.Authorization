export interface IRole {
  id: string;
  parentRole: string;
  childRoles: Array<string>;
  name: string;
  grain: string;
  securableItem: string;
}
