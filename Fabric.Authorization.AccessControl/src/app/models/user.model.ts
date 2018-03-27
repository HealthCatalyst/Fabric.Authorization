import { Role, Group } from '../models';

export class User {
  public id: string;
  public groups: Array<Group>;
  public roles: Array<Role>;
  public name: string;

  constructor(public identityProvider: string, public subjectId: string) {}
}
