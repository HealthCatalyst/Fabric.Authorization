import { Injectable } from '@angular/core';

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
