import { IRole } from './role.model';

export type AuthMemberSearchResultEntityType = 'User' | 'DirectoryGroup' | 'CustomGroup';

export interface IAuthMemberSearchResult {
  subjectId: string;
  identityProvider: string;
  roles: Array<IRole>;
  groupName: string;
  firstName: string;
  middleName: string;
  lastName: string;
  lastLoginDateTimeUtc?: string | Date;
  entityType: AuthMemberSearchResultEntityType;
  name?: string;
}
