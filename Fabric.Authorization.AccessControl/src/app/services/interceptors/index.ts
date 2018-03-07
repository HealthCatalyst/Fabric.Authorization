/* "Barrel" of Http Interceptors */
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { FabricHttpRequestInterceptorService } from './fabric-http-request-interceptor.service';
import { FabricHttpErrorHandlerInterceptorService } from './fabric-http-error-handler-interceptor.service';

/** Http interceptor providers in outside-in order */
export const httpInterceptorProviders = [
  { provide: HTTP_INTERCEPTORS, useClass: FabricHttpRequestInterceptorService, multi: true },
  { provide: HTTP_INTERCEPTORS, useClass: FabricHttpErrorHandlerInterceptorService, multi: true }
];