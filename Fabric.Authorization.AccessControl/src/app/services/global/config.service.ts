import { Observable } from 'rxjs/Observable';
import { Injectable } from '@angular/core';

function getWindow(): any {
    return window;
}

@Injectable()
export class ConfigService {

  constructor() { }

  public getDiscoveryServiceRoot(): Observable<string> {
    return Observable.of(getWindow().discoveryServiceRoot);
  }
}
