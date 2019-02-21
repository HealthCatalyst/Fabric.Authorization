import { TestBed, inject } from '@angular/core/testing';

import { FabricHttpFakeDiscoveryInterceptorService } from './fabric-http-fake-discovery-interceptor.service';

describe('FabricHttpFakeDiscoveryInterceptorService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [FabricHttpFakeDiscoveryInterceptorService]
    });
  });

  it('should be created', inject([FabricHttpFakeDiscoveryInterceptorService], (service: FabricHttpFakeDiscoveryInterceptorService) => {
    expect(service).toBeTruthy();
  }));
});
