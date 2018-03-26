import { Injectable } from '@angular/core';
<<<<<<< HEAD
import { environment } from '../../environments/environment';
=======

>>>>>>> remove environments import in config service

@Injectable()
export class AccessControlConfigService {
  clientId: string;
  identityProvider: string;
  grain: string;
  securableItem: string;

  constructor() {}

  getAccessToken(): Promise<string> {
    return Promise.resolve('');
  }

  getFabricAuthApiUrl(): string {
    return '';
  }

  getFabricExternalIdpSearchApiUrl(): string {
    return '';
  }
}
