import {
  HTTP_INTERCEPTORS
} from '@angular/common/http';
import {
  HttpClientTestingModule
} from '@angular/common/http/testing';
import { FabricHttpErrorHandlerInterceptorService } from './interceptors/fabric-http-error-handler-interceptor.service';
import { TestBed, inject } from '@angular/core/testing';

import { FabricAuthGrainService } from './fabric-auth-grain.service';
import { MockAccessControlConfigService } from './access-control-config.service.mock';

describe('FabricAuthGrainService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [FabricAuthGrainService,
        {
          provide: HTTP_INTERCEPTORS,
          useClass: FabricHttpErrorHandlerInterceptorService,
          multi: true
        },
        {
          provide: 'IAccessControlConfigService',
          useClass: MockAccessControlConfigService
        }]
    });
  });

  it('should be created', inject([FabricAuthGrainService], (service: FabricAuthGrainService) => {
    expect(service).toBeTruthy();
  }));
});
