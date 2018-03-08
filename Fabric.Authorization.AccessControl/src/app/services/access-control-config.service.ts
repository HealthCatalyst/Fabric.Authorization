import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment.test';

@Injectable()
export class AccessControlConfigService {

  clientId: string;
  fabricAuthorizationBaseUrl: string;
  fabricIdpSearchServiceBaseUrl: string;

  constructor() {

  }

  getAccessToken() : Promise<string>{
    return Promise.reject('');
  }

  getFabricAuthApiUrl() : string {
    console.log(`${environment.fabricAuthApiUri}/${environment.fabricAuthApiVersionSegment}`);
    return `${environment.fabricAuthApiUri}/${environment.fabricAuthApiVersionSegment}`
  }

  getFabricExternalIdpSearchApiUrl() : string {
    return `${environment.fabricExternalIdPSearchApiUri}/${environment.fabricExternalIdPSearchApiVersionSegment}`
  }
}
