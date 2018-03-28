import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';

@Injectable()
export class AccessControlConfigService {
  clientId: string;
  identityProvider: string;
  grain: string;
  securableItem: string;

  constructor() {}

  getAccessToken(): Promise<string> {
    return Promise.resolve('');
  }

  getFabricAuthApiUrl(): string {
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
