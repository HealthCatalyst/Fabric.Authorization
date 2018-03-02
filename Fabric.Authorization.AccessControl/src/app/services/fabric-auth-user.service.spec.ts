import { TestBed, inject } from '@angular/core/testing';

import { FabricAuthUserService } from './fabric-auth-user.service';

describe('FabricAuthUserService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [FabricAuthUserService]
    });
  });

  it('should be created', inject([FabricAuthUserService], (service: FabricAuthUserService) => {
    expect(service).toBeTruthy();
  }));
});
