import { Role } from '../models';

export type AuthMemberSearchResultEntityType = 'User' | 'DirectoryGroup' | 'CustomGroup';

export interface AuthMemberSearchResult {
  subjectId: string;
  identityProvider: string;
  roles: Array<Role>;
  groupName: string;
  firstName: string;
  middleName: string;
  lastName: string;
  lastLoginDateTimeUtc: string | Date;
  entityType: AuthMemberSearchResultEntityType;
  name: string;
}
