import { Injectable } from '@angular/core';
import { FabricAuthUserService } from './fabric-auth-user.service';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class CurrentUserService {
  private permissions: Array<string> = [];
  private lastUpdateTimestamp = 0;

  constructor(private authUserService: FabricAuthUserService) {
  }

  getPermissions(): Observable<string[]> {
    const currentTimeStamp = new Date().getTime();
    if (this.lastUpdateTimestamp === 0 || (Math.abs(currentTimeStamp - this.lastUpdateTimestamp) / 60000) > 2) {
      this.lastUpdateTimestamp = currentTimeStamp;
      return this.authUserService.getCurrentUserPermissions().do(userPermissionResponse => {
        this.permissions = userPermissionResponse.permissions;
      }).map(p => p.permissions);
    }

    return Observable.of(this.permissions);
  }
}
