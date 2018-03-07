import { TestBed, inject } from '@angular/core/testing';

import { FabricHttpErrorHandlerInterceptorService } from './fabric-http-error-handler-interceptor.service';

describe('FabricHttpErrorHandlerInterceptorService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [FabricHttpErrorHandlerInterceptorService]
    });
  });

  it('should be created', inject([FabricHttpErrorHandlerInterceptorService], (service: FabricHttpErrorHandlerInterceptorService) => {
    expect(service).toBeTruthy();
  }));
});
