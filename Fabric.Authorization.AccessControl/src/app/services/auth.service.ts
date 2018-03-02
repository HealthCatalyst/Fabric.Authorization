import { Injectable } from '@angular/core';
import { Response, Http, Headers, RequestOptions } from '@angular/http';
import { UserManager, User, Log} from 'oidc-client';
import { Observable } from 'rxjs';

import { ConfigService } from './config.service';
import { Config } from '../models/config';
import { LoggingService } from './logging.service';


@Injectable()
export class AuthService {
  userManager: UserManager; 
  configSettings: Config;
  identityClientSettings: any;
  clientId: string;

  constructor(private configService: ConfigService, private loggingService: LoggingService, private http: Http) {
    this.configSettings = configService.config;
    this.clientId = 'fabric-angularsample';
    
    Log.logger = loggingService;    
    
    var self = this;

    const clientSettings: any = {
      authority: this.configSettings.authority,
      client_id: this.clientId,
      redirect_uri: this.configSettings.redirectUri,
      post_logout_redirect_uri: this.configSettings.postLogoutRedirectUri,
      response_type: 'id_token token',
      scope: this.configSettings.scope,  
      silent_redirect_uri: this.configSettings.silentRedirectUri,
      automaticSilentRenew: true,    
      filterProtocolClaims: true,
      loadUserInfo: true
    };

    this.userManager = new UserManager(clientSettings);    

    this.userManager.events.addAccessTokenExpiring(function(){      
      loggingService.log("access token expiring");
    });

    this.userManager.events.addSilentRenewError(function(e){
      loggingService.log("silent renew error: " + e.message);
    });

    this.userManager.events.addAccessTokenExpired(function () {
      loggingService.log("access token expired");    
      //when access token expires logout the user
      self.logout();
    });  
   }


  login() {
    var self = this;
    this.userManager.signinRedirect().then(() => {
      self.loggingService.log("signin redirect done");
    }).catch(err => {
      self.loggingService.error(err);
    });
  }

  logout() {
    this.userManager.signoutRedirect();
  }

  handleSigninRedirectCallback() {
    var self = this;
    this.userManager.signinRedirectCallback().then(user => {
      if (user) {
        self.loggingService.log("Logged in: " + JSON.stringify(user.profile));
      } else {
        self.loggingService.log("could not log user in");
      }
    }).catch(e => {
      self.loggingService.error(e);
    });
  }

  getUser(): Promise<User> {
    return this.userManager.getUser();
  }

  isUserAuthenticated() {
    var self = this;
    return this.userManager.getUser().then(function (user) {
      if (user) {
        self.loggingService.log("signin redirect done. ");
        self.loggingService.log(user.profile);
        return true;
      } else {
        self.loggingService.log("User is not logged in");
        return false;
      }
    });
  }
  
  private getAccessToken() : Promise<string>{
    let self = this;
    return this.getUser()
       .then(function(user){           
       if(user){
            return Promise.resolve(user.access_token);
           }
       });
  }

  private handleError (error: Response | any) {
    this.loggingService.error('Error Response:');
    this.loggingService.error(error.message || error);
    return Observable.throw(error.message || error);
  }

  get<T>(resource: string) : Promise<T>{
    return this.getAccessToken()
    .then((token)=>{
        let headers = new Headers({ 'Authorization': 'Bearer ' + token });
        let options = new RequestOptions({ headers: headers });
        let requestUrl = this.configSettings.authority + '/' + resource;           
        return this.http.get(requestUrl, options)
            .map((res: Response) => {                                         
            return res.json();
            })
            .catch(error => this.handleError(error))
            .toPromise<T>()
    });        
} 

}

