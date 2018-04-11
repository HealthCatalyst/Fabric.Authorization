import { IRole, IGroup } from '../models';

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
