import { Injectable } from '@angular/core';
import { AuthService } from './auth.service';
import { AccessControlConfigService } from '../access-control-config.service';
import { environment } from '../../../environments/environment';
import { IDataChanged } from '../../models';

@Injectable()
export class ClientAccessControlConfigService
  implements AccessControlConfigService {

  constructor(private authService: AuthService) {}

  clientId = '';
  identityProvider = 'windows';
  grain = 'dos';
  securableItem = 'datamarts';
  fabricAuthApiUrl = `${environment.fabricAuthApiUri}/${environment.fabricAuthApiVersionSegment}`;
  fabricExternalIdpSearchApiUrl = `${environment.fabricExternalIdPSearchApiUri}/${environment.fabricExternalIdPSearchApiVersionSegment}`;

  dataChangeEvent(eventArgs: IDataChanged): void {
    console.log(`some data changed: ${JSON.stringify(eventArgs)}`);
  }
}
