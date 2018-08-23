import { IRole } from './role.model';
import { IUser } from './user.model';

export interface IGroup {
  id?: string;
  roles?: Array<IRole>;
  users?: Array<IUser>;
  groupName: string;
  groupSource: string;
  displayName?: string;
  description?: string;
  children?: Array<IGroup>;
  parents?: Array<IGroup>;

   // Custom Property
   selected?: boolean;
}
