import { Injectable } from '@angular/core';
import { AuthService } from './auth.service';

@Injectable()
export class ClientAccessControlConfigService {

  constructor(private authService: AuthService) { }

  clientId = 'fabric-angularsample';

  getAccessToken(){
     return this.authService.getUser()
    .then(function(user){           
    if(user){
         return Promise.resolve(user.access_token);
        }
    });
  }
}
