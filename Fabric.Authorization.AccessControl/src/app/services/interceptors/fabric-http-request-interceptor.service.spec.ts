import { TestBed, inject } from '@angular/core/testing';

import { FabricHttpRequestInterceptorService } from './fabric-http-request-interceptor.service';

describe('FabricHttpRequestInterceptorService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [FabricHttpRequestInterceptorService]
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
