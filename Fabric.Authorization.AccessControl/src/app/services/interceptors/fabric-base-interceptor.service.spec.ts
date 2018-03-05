import { TestBed, inject } from '@angular/core/testing';

import { FabricBaseInterceptorService } from './fabric-base-interceptor.service';

describe('FabricBaseInterceptorService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [FabricBaseInterceptorService]
    });
  });

  it('should be created', inject([FabricBaseInterceptorService], (service: FabricBaseInterceptorService) => {
    expect(service).toBeTruthy();
  }));
});
