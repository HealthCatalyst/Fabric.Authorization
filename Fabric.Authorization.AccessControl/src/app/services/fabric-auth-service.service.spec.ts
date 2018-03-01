import { TestBed, inject } from '@angular/core/testing';

import { FabricAuthServiceService } from './fabric-auth-service.service';

describe('FabricAuthServiceService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [FabricAuthServiceService]
    });
  });

  it('should be created', inject([FabricAuthServiceService], (service: FabricAuthServiceService) => {
    expect(service).toBeTruthy();
  }));
});
