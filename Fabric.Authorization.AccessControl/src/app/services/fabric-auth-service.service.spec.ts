import { TestBed, inject } from '@angular/core/testing';

import { FabricAuthService } from './fabric-auth-service.service';

describe('FabricAuthServiceService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [FabricAuthService]
    });
  });

  it('should be created', inject([FabricAuthService], (service: FabricAuthService) => {
    expect(service).toBeTruthy();
  }));
});
