import { IRole } from './role.model';

export interface IAuthMemberSearchResult {
  subjectId: string;
  identityProvider: string;
  roles: Array<IRole>;
  groupName: string;
  firstName: string;
  middleName: string;
  lastName: string;
  lastLoginDateTimeUtc: string | Date;
  entityType: string;
  name: string;
}
