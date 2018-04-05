import { Injectable } from '@angular/core';
import { AuthService } from './auth.service';
import { AccessControlConfigService } from '../access-control-config.service';
import { environment } from '../../../environments/environment';

@Injectable()
export class ClientAccessControlConfigService
  implements AccessControlConfigService {
  constructor(private authService: AuthService) {}

  clientId = '';
  identityProvider = 'windows';
  grain = 'dos';
  securableItem = 'datamarts';

  getFabricAuthApiUrl(): string {
    console.log(
      `${environment.fabricAuthApiUri}/${
        environment.fabricAuthApiVersionSegment
      }`
    );
    return `${environment.fabricAuthApiUri}/${
      environment.fabricAuthApiVersionSegment
    }`;
  }

  getFabricExternalIdpSearchApiUrl(): string {
    return `${environment.fabricExternalIdPSearchApiUri}/${
      environment.fabricExternalIdPSearchApiVersionSegment
    }`;
  }
}
