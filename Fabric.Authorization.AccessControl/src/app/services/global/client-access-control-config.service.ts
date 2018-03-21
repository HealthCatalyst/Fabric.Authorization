import { Injectable } from '@angular/core';
import { AuthService } from './auth.service';
import { AccessControlConfigService } from '../access-control-config.service';
import { environment } from '../../../environments/environment.prod';

@Injectable()
export class ClientAccessControlConfigService implements AccessControlConfigService {


  constructor(private authService: AuthService) { }

  clientId = 'fabric-angularsample';
  identityProvider = 'windows';
  grain = 'dos';
  securableItem = 'datamarts';
  
  getAccessToken(){
     return this.authService.getUser()
    .then(function(user){           
    if(user){
         return user.access_token;
        }
    });
  }

  getFabricAuthApiUrl() : string {
    console.log(`${environment.fabricAuthApiUri}/${environment.fabricAuthApiVersionSegment}`);
    return `${environment.fabricAuthApiUri}/${environment.fabricAuthApiVersionSegment}`
  }

  getFabricExternalIdpSearchApiUrl() : string {
    return `${environment.fabricExternalIdPSearchApiUri}/${environment.fabricExternalIdPSearchApiVersionSegment}`
  }
}
