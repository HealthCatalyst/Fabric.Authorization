import { Injectable } from '@angular/core';
import { Observable, of, throwError } from 'rxjs';
import {
  HttpInterceptor,
  HttpRequest,
  HttpResponse,
  HttpHandler,
  HttpEvent
} from '@angular/common/http';
import { ConfigService } from '../global/config.service';

@Injectable()
export class FabricHttpFakeDiscoveryInterceptorService implements HttpInterceptor {
  private responseBody : any = null;

  constructor(private configService: ConfigService) { }

  intercept(
    req: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    if(req.url.includes('DiscoveryService/v1/Services?$filter=ServiceName')){
        this.configService.getIdentityServiceRoot().subscribe(url => {
          if(url === '') { // this can happen if windows auth is on
            url = "http://localhost/identity"
          }

          this.responseBody = {
            "@odata.context":"http://localhost/DiscoveryService/v1/$metadata#Services(ServiceUrl,Version,ServiceName)","value":[
              {
                "ServiceUrl":"http://localhost/IdentityProviderSearchService/v1","Version":1,"ServiceName":"IdentityProviderSearchService"
              },{
                "ServiceUrl":url,"Version":1,"ServiceName":"IdentityService"
              },{
                "ServiceUrl":"http://localhost/AuthorizationDev/v1","Version":1,"ServiceName":"AuthorizationService"
              },{
                "ServiceUrl":"http://localhost/AuthorizationDev","Version":1,"ServiceName":"AccessControl"
              }
            ]
          };
        })
        return of(new HttpResponse({status: 200, body: this.responseBody}));
      
    }else{
      return next.handle(req);
    }
  }
}
