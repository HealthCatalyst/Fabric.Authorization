import { Observable, of } from 'rxjs';
import { Injectable } from '@angular/core';

function getWindow(): any {
    return window;
}

@Injectable()
export class ConfigService {

  constructor() { }

  public getUseOAuthAuthentication(): Observable<string> {
    return of(getWindow().useOAuthAuthentication);
  }

  public getDiscoveryServiceRoot(): Observable<string> {
    return of(getWindow().discoveryServiceRoot);
  }

  public getIdentityServiceRoot(): Observable<string> {
    return of(getWindow().identityServiceRoot);
  }

  public getAccessControlServiceRoot(): Observable<string> {
    return of(getWindow().accessControlServiceRoot);
  }
}
