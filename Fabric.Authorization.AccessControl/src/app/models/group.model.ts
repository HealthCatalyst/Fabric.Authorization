import { IRole, IUser } from '../models';

export interface IGroup {
  id?: string;
  roles?: Array<IRole>;
  users?: Array<IUser>;
  groupName: string;
  groupSource: string;
  displayName?: string;
  description?: string;
}
