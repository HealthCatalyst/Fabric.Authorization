import { Injectable } from '@angular/core';
import { Response, Http, Headers, RequestOptions } from '@angular/http';
import { UserManager, User, Log} from 'oidc-client';
import { Observable } from 'rxjs';
import { log } from 'util';

@Injectable()
export class AuthService {
  userManager: UserManager;   
  identityClientSettings: any;
  clientId: string;
  authority: string;

  constructor(private http: Http) {   
    this.clientId = 'fabric-accesscontrolsample';
    this.authority = 'http://localhost:5001';
    
    var self = this;

    const clientSettings: any = {
      authority: this.authority,
      client_id: this.clientId,
      redirect_uri: 'http://localhost:4200/oidc-callback.html',
      post_logout_redirect_uri: 'http://localhost:4200',
      response_type: 'id_token token',
      scope: 'openid profile fabric.profile patientapi fabric/authorization.read fabric/authorization.write fabric/identity.manageresources fabric/identity.read fabric/authorization.manageclients',  
      silent_redirect_uri: 'http://localhost:4200/silent.html',
      automaticSilentRenew: true,    
      filterProtocolClaims: true,
      loadUserInfo: true
    };

    this.userManager = new UserManager(clientSettings);    

    this.userManager.events.addAccessTokenExpiring(function(){      
      Log.debug('access token expiring');
    });

    this.userManager.events.addSilentRenewError(function(e){
      Log.debug('silent renew error: ' + e.message);
    });

    this.userManager.events.addAccessTokenExpired(function () {
      Log.debug('access token expired');    
      //when access token expires logout the user
      self.logout();
    });  
   }


  login() {
    var self = this;
    this.userManager.signinRedirect().then(() => {
      Log.debug('signin redirect done');
    }).catch(err => {
      Log.debug(err);
    });
  }

  logout() {
    this.userManager.signoutRedirect();
  }

  handleSigninRedirectCallback() {
    var self = this;
    this.userManager.signinRedirectCallback().then(user => {
      if (user) {
        Log.debug('Logged in: ' + JSON.stringify(user.profile));
      } else {
        Log.debug('could not log user in');
      }
    }).catch(e => {
      Log.error(e);
    });
  }

  getUser(): Promise<User> {
    return this.userManager.getUser();
  }

  isUserAuthenticated() {
    var self = this;
    return this.userManager.getUser().then(function (user) {
      if (user) {
        Log.debug('signin redirect done. ');
        Log.debug(user.profile);
        return true;
      } else {
        Log.debug('User is not logged in');
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
    Log.error('Error Response:');
    Log.error(error.message || error);
    return Observable.throw(error.message || error);
  }

  get<T>(resource: string) : Promise<T>{
    return this.getAccessToken()
    .then((token)=>{
        let headers = new Headers({ 'Authorization': 'Bearer ' + token });
        let options = new RequestOptions({ headers: headers });
        let requestUrl = this.authority + '/' + resource;           
        return this.http.get(requestUrl, options)
            .map((res: Response) => {                                         
            return res.json();
            })
            .catch(error => this.handleError(error))
            .toPromise<T>()
    });        
} 

}

