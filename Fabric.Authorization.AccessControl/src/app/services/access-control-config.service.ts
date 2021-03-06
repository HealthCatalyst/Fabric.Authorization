import { IDataChangedEventArgs } from '../models/changedDataEventArgs.model';
import { Subject, Observable } from 'rxjs';
import { Exception } from '../models/exception.model';

export interface IAccessControlConfigService {
  clientId: string;
  identityProvider: string;
  grain: string;
  securableItem: string;
  fabricAuthApiUrl: string;
  fabricExternalIdpSearchApiUrl: Observable<string>;
  dataChanged: Subject<IDataChangedEventArgs>;
  errorRaised: Subject<Exception>;
}
