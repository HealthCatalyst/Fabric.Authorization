import { Injectable } from '@angular/core';
import { IDataChangedEventArgs } from '../models';

@Injectable()
export class AccessControlConfigService {
  clientId: string;
  identityProvider: string;
  grain: string;
  securableItem: string;
  fabricAuthApiUrl: string;
  fabricExternalIdpSearchApiUrl: string;

  dataChangedEvent(eventArgs: IDataChangedEventArgs) {}

  constructor() {}
}
