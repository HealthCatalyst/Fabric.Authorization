import { Injectable } from '@angular/core';

@Injectable()
export class AccessControlConfigService {
  clientId: string;
  identityProvider: string;
  grain: string;
  securableItem: string;
  fabricAuthApiUrl: string;
  fabricExternalIdpSearchApiUrl: string;

  dataChangeEvent(eventArgs) {}

  constructor() {}
}
