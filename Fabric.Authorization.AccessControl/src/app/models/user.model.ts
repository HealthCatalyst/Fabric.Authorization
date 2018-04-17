import { IGroup } from './group.model';
import { IRole } from './role.model';

export interface IUser {
  id?: string;
  groups?: Array<IGroup>;
  roles?: Array<IRole>;
  name?: string;
  identityProvider?: string;
  subjectId: string;

  // custom property
  selected?: boolean;
}
