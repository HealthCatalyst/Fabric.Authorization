import { Injectable } from '@angular/core';
import { IDataChangedEventArgs } from '../models/changedDataEventArgs.model';
import { Observable } from 'rxjs/Observable';
import { Subject } from 'rxjs/Subject';
import { Exception } from '../models/exception.model';

export interface IAccessControlConfigService {
  clientId: string;
  identityProvider: string;
  grain: string;
  securableItem: string;
  fabricAuthApiUrl: string;
  fabricExternalIdpSearchApiUrl: string;
  dataChanged: Subject<IDataChangedEventArgs>;
  errorRaised: Subject<Exception>;
}
