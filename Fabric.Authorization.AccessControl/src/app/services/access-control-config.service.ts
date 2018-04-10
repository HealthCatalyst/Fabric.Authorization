import { Injectable } from '@angular/core';
import { IDataChanged } from '../models';

@Injectable()
export class AccessControlConfigService {
  clientId: string;
  identityProvider: string;
  grain: string;
  securableItem: string;
  fabricAuthApiUrl: string;
  fabricExternalIdpSearchApiUrl: string;

  dataChangeEvent(eventArgs: IDataChanged) {}

  constructor() {}
}
