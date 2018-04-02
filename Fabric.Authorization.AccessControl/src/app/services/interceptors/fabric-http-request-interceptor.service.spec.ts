import { TestBed, inject } from '@angular/core/testing';

import { FabricHttpRequestInterceptorService } from './fabric-http-request-interceptor.service';
import { AccessControlConfigService } from '..';

describe('FabricHttpRequestInterceptorService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [FabricHttpRequestInterceptorService, AccessControlConfigService]
    });
  });

  it(
    'should be created',
    inject(
      [FabricHttpRequestInterceptorService],
      (service: FabricHttpRequestInterceptorService) => {
        expect(service).toBeTruthy();
      }
    )
  );
});
