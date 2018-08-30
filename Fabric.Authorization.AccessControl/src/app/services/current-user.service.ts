import { Injectable } from '@angular/core';
import { FabricAuthUserService } from './fabric-auth-user.service';
import { Observable } from '../../../node_modules/rxjs';

@Injectable({
  providedIn: 'root'
})
export class CurrentUserService {
  private permissions: Array<string> = [];
  private initialized: boolean;

  constructor(private authUserService: FabricAuthUserService) {
  }

  getPermissions(): Observable<string[]> {
    if (!this.initialized) {
      this.initialized = true;
      return this.authUserService.getCurrentUserPermissions().do(userPermissionResponse => {
        this.permissions = userPermissionResponse.permissions;
      }).map(p => p.permissions);
    }

    return Observable.of(this.permissions);
  }
}
