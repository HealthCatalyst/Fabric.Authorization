import { IPermissionRequestContext } from './permissionRequestContext.model';

export interface IUserPermissionResponse {
  permissions: Array<string>;
  permissionRequestContexts: Array<IPermissionRequestContext>;
}
