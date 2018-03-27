import { Role, User } from '../models';

export class Group {
  public id: string;
  public roles: Array<Role>;
  public users: Array<User> = new Array<User>();

  constructor(public groupName: string, public groupSource: string) {}
}
