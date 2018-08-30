import {
  HTTP_INTERCEPTORS
} from '@angular/common/http';
import { TestBed, inject } from '@angular/core/testing';
import {
  HttpClientTestingModule
} from '@angular/common/http/testing';

import { CurrentUserService } from './current-user.service';
import { FabricAuthUserService } from './fabric-auth-user.service';
import { MockAccessControlConfigService } from './access-control-config.service.mock';
import { FabricHttpErrorHandlerInterceptorService } from './interceptors/fabric-http-error-handler-interceptor.service';

describe('CurrentUserService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        CurrentUserService,
        FabricAuthUserService,
        {
          provide: HTTP_INTERCEPTORS,
          useClass: FabricHttpErrorHandlerInterceptorService,
          multi: true
        },
        {
          provide: 'IAccessControlConfigService',
          useClass: MockAccessControlConfigService
        }
      ]
    });
  });

  it('should be created', inject([CurrentUserService], (service: CurrentUserService) => {
    expect(service).toBeTruthy();
  }));
});
