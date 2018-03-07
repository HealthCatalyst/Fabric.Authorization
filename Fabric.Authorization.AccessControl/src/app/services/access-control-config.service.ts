import { Injectable } from '@angular/core';

@Injectable()
export class AccessControlConfigService {

  constructor() { }

  clientId: string;

  getAccessToken() : Promise<string>{
    return Promise.reject('');
  }

}
