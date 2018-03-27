import { TestBed, inject } from '@angular/core/testing';

import { FabricAuthRoleService } from './fabric-auth-role.service';

describe('FabricAuthRoleService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [FabricAuthRoleService]
    });
  });

  it(
    'should be created',
    inject([FabricAuthRoleService], (service: FabricAuthRoleService) => {
      expect(service).toBeTruthy();
    })
  );
});
