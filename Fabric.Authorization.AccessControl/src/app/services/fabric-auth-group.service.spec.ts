import { TestBed, inject } from '@angular/core/testing';

import { FabricAuthGroupService } from './fabric-auth-group.service';

describe('FabricAuthGroupService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [FabricAuthGroupService]
    });
  });

  it('should be created', inject([FabricAuthGroupService], (service: FabricAuthGroupService) => {
    expect(service).toBeTruthy();
  }));
});
