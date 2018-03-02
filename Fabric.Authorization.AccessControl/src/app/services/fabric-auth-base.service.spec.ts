import { TestBed, inject } from '@angular/core/testing';

import { FabricAuthBaseService } from './fabric-auth-base.service';

describe('FabricAuthBaseService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [FabricAuthBaseService]
    });
  });

  it('should be created', inject([FabricAuthBaseService], (service: FabricAuthBaseService) => {
    expect(service).toBeTruthy();
  }));
});
