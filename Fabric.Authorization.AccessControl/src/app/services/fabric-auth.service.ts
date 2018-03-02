import { Injectable } from '@angular/core';
import { Response, Http, Headers, RequestOptions } from '@angular/http';
import { Observable } from 'rxjs';

import { HttpInterceptor } from './custom-http.service';
import { LoggingService } from './logging.service'
import { AuthService } from './auth.service';
import { ConfigService } from './config.service';

import { Config } from '../models/config';

@Injectable()
export class FabricAuthService{
    private _uriBase: string;
    private _appConfig: Config;
    
    constructor(private _http: Http, private _authService: AuthService, private _configService: ConfigService, private _loggingService: LoggingService) { 
        this._appConfig = _configService.config;
        this._uriBase = this._appConfig.authorization;
    }
    
    getPermissionsForUser(): Promise<UserPermissions> {       
        return this.get(`user/permissions`);
    }    
    
    get<T>(resource: string) : Promise<T>{
        return this.getAccessToken()
        .then((token)=>{
            let headers = new Headers({ 'Authorization': 'Bearer ' + token });
            let options = new RequestOptions({ headers: headers });
            let requestUrl = this._uriBase + '/' + resource;           
            return this._http.get(requestUrl, options)
                .map((res: Response) => {                                         
                return res.json();
                })
                .catch(error => this.handleError(error))
                .toPromise<T>()
        });        
    }    

    post<T>(data: any, resource: string ) : Promise<T>{
        return this.getAccessToken()
        .then(token => {
            let headers = new Headers({ 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + token });
            let options = new RequestOptions({ headers: headers });
            let requestUrl = this._uriBase + '/' + resource;           
            return this._http.post(requestUrl, data, options)
              .map((res: Response) => {               
                return res.json();
              })
              .catch(error => this.handleError(error))
              .toPromise<T>();
          });        
    }

    getObservable(resource: string){
        return this.getAccessToken()
        .then((token)=>{
            let headers = new Headers({ 'Authorization': 'Bearer ' + token });
            let options = new RequestOptions({ headers: headers });
            return this._http.get(resource, options);
        });    
    }

    private getAccessToken() : Promise<string>{
         let self = this;
         return this._authService.getUser()
            .then(function(user){           
            if(user){
                 return Promise.resolve(user.access_token);
                }
            });
    }

    private handleError (error: Response | any) {
      this._loggingService.error('Error Response:');
      this._loggingService.error(error.message || error);
      return Observable.throw(error.message || error);
    }
}
interface UserPermissions{
    permissions: string[],
    requestedGrain: string,
    requestedSecurableItem: string
}

