import { Injectable } from '@angular/core';

@Injectable()
export class AccessControlConfigService {
  clientId: string;
  identityProvider: string;
  grain: string;
  securableItem: string;

  constructor() {}

  getFabricAuthApiUrl(): string {
    return '';
  }

  getFabricExternalIdpSearchApiUrl(): string {
    return '';
  }
}
