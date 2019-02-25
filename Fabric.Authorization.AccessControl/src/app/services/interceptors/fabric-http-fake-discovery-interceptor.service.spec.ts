import { TestBed, inject } from '@angular/core/testing';

import { FabricHttpFakeDiscoveryInterceptorService } from './fabric-http-fake-discovery-interceptor.service';
import { ConfigService } from '../global/config.service';

describe('FabricHttpFakeDiscoveryInterceptorService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        FabricHttpFakeDiscoveryInterceptorService,
        ConfigService
      ]
    });
  });

  it('should be created', inject([FabricHttpFakeDiscoveryInterceptorService], (service: FabricHttpFakeDiscoveryInterceptorService) => {
    expect(service).toBeTruthy();
  }));
});
