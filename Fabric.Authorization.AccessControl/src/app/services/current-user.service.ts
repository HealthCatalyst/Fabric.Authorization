
import { map, tap } from 'rxjs/operators';
import { Injectable } from '@angular/core';
import { FabricAuthUserService } from './fabric-auth-user.service';
import { Observable, of } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class CurrentUserService {
  private permissionMap: Map<string, Array<string>> = new Map<string, Array<string>>();
  private lastUpdateTimestampMap: Map<string, number> = new Map<string, number>();

  constructor(private authUserService: FabricAuthUserService) {
  }

  getPermissions(securableItem: string): Observable<string[]> {
    const currentTimeStamp = new Date().getTime();
    if (this.shouldRefreshCache(securableItem, currentTimeStamp)) {
      return this.authUserService.getCurrentUserPermissions(securableItem).pipe(
         tap(userPermissionResponse => {
          this.lastUpdateTimestampMap.set(securableItem, currentTimeStamp);
          this.permissionMap.set(securableItem, userPermissionResponse.permissions);
        }),
        map(p => p.permissions)
      );
    }

    return of(this.permissionMap.get(securableItem));
  }

  resetPermissionCache(securableItem: string) {
    this.lastUpdateTimestampMap.set(securableItem, 0);
  }

  private shouldRefreshCache(securableItem: string, currentTimeStamp: number): boolean {
    return !this.lastUpdateTimestampMap.has(securableItem)
        || this.lastUpdateTimestampMap.get(securableItem) === 0
        || (Math.abs(currentTimeStamp - this.lastUpdateTimestampMap.get(securableItem)) / 60000) > 2;
  }
}
