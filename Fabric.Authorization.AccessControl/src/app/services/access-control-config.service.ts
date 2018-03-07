import { Injectable } from '@angular/core';

@Injectable()
export class AccessControlConfigService {

  constructor() { }

  clientId: string;
  fabricAuthorizationBaseUrl: string;
  fabricIdpSearchServiceBaseUrl: string;

  getAccessToken() : Promise<string>{
    return Promise.reject('');
  }

}
