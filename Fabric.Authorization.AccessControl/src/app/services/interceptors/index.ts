/* "Barrel" of Http Interceptors */
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { FabricHttpRequestInterceptorService } from './fabric-http-request-interceptor.service';
import { FabricHttpErrorHandlerInterceptorService } from './fabric-http-error-handler-interceptor.service';
import { FabricHttpFakeDiscoveryInterceptorService } from './fabric-http-fake-discovery-interceptor.service';
import { environment } from '../../../environments/environment'

/** Http interceptor providers in outside-in order */
export const httpInterceptorProviders = [];
if (!environment.production) {
  httpInterceptorProviders.push({
    provide: HTTP_INTERCEPTORS,
    useClass: FabricHttpFakeDiscoveryInterceptorService,
    multi: true
  });
}
httpInterceptorProviders.push({
  provide: HTTP_INTERCEPTORS,
  useClass: FabricHttpRequestInterceptorService,
  multi: true
});
httpInterceptorProviders.push({
  provide: HTTP_INTERCEPTORS,
  useClass: FabricHttpErrorHandlerInterceptorService,
  multi: true
});
