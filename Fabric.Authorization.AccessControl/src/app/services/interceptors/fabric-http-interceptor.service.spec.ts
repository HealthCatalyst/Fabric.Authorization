import { TestBed, inject } from '@angular/core/testing';

import { FabricHttpInterceptorService } from './fabric-http-interceptor.service';

describe('FabricHttpInterceptorService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [FabricHttpInterceptorService]
    });
  });

  it('should be created', inject([FabricHttpInterceptorService], (service: FabricHttpInterceptorService) => {
    expect(service).toBeTruthy();
  }));
});
