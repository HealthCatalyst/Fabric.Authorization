import { Injectable } from '@angular/core';
import { AuthService } from './auth.service';
import { IAccessControlConfigService } from '../access-control-config.service';
import { environment } from '../../../environments/environment';
import { IDataChangedEventArgs } from '../../models/changedDataEventArgs.model';
import { Observable } from 'rxjs/Observable';
import { Subject } from 'rxjs/Subject';
import { Exception } from '../../models/exception.model';

@Injectable()
export class ClientAccessControlConfigService implements IAccessControlConfigService {
  dataChanged = new Subject<IDataChangedEventArgs>();
  errorRaised = new Subject<Exception>();

  constructor(private authService: AuthService) {
    this.dataChanged.subscribe((eventArgs: IDataChangedEventArgs) => {
      console.log(`some data changed: ${JSON.stringify(eventArgs)}`);
    });

    this.errorRaised.subscribe((eventArgs: Exception) => {
      console.log(`error: ${JSON.stringify(eventArgs)}`);
    });
  }

  clientId = '';
  identityProvider = 'windows';
  grain = 'dos';
  securableItem = 'datamarts';
  fabricAuthApiUrl = `${environment.fabricAuthApiUri}/${environment.fabricAuthApiVersionSegment}`;
  fabricExternalIdpSearchApiUrl = `${environment.fabricExternalIdPSearchApiUri}/${environment.fabricExternalIdPSearchApiVersionSegment}`;
}
