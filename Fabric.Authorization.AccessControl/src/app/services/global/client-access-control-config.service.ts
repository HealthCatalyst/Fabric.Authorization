import { Injectable } from '@angular/core';
import { AuthService } from './auth.service';
import { IAccessControlConfigService } from '../access-control-config.service';
import { IDataChangedEventArgs } from '../../models/changedDataEventArgs.model';
import { Subject, Observable } from 'rxjs';
import { Exception } from '../../models/exception.model';
import { ServicesService } from './services.service';
import { switchMap } from 'rxjs/operators';

@Injectable()
export class ClientAccessControlConfigService implements IAccessControlConfigService {
  dataChanged = new Subject<IDataChangedEventArgs>();
  errorRaised = new Subject<Exception>();

  constructor(private authService: AuthService, private servicesService: ServicesService) {
    this.dataChanged.subscribe((eventArgs: IDataChangedEventArgs) => {
      console.log(`some data changed: ${JSON.stringify(eventArgs)}`);
    });

    this.errorRaised.subscribe((eventArgs: Exception) => {
      console.log(`error: ${JSON.stringify(eventArgs)}`);
    });

    this.fabricAuthApiUrl = this.servicesService.authorizationServiceEndpoint
    this.fabricExternalIdpSearchApiUrl = this.servicesService.identityProviderSearchServiceEndpoint
  }

  clientId = '';
  identityProvider = 'windows';
  grain = 'dos';
  securableItem = 'datamarts';
  fabricAuthApiUrl = null
  fabricExternalIdpSearchApiUrl = null
}
