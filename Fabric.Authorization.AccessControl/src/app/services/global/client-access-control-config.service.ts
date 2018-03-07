import { Injectable } from '@angular/core';
import { AuthService } from './auth.service';
import { AccessControlConfigService } from '../access-control-config.service';

@Injectable()
export class ClientAccessControlConfigService implements AccessControlConfigService {

  constructor(private authService: AuthService) { }

  clientId = 'fabric-angularsample';
  fabricAuthorizationBaseUrl = '';
  fabricIdpSearchServiceBaseUrl = '';

  getAccessToken(){
     return this.authService.getUser()
    .then(function(user){           
    if(user){
         return user.access_token;
        }
    });
  }
}
