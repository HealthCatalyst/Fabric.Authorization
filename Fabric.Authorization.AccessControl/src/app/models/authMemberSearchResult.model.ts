import { Role } from './role.model';

export class AuthMemberSearchResult {
  public subjectId: string;
  public identityProvider: string;
  public roles: Array<Role>;
  public groupName: string;
  public firstName: string;
  public middleName: string;
  public lastName: string;
  public lastLoginDateTimeUtc: string | Date;
  public entityType: string;
  public name: string;

  constructor() {}
}
