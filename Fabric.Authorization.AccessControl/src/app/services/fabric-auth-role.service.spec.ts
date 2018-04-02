import { TestBed, inject } from '@angular/core/testing';

import { FabricAuthRoleService } from './fabric-auth-role.service';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { AccessControlConfigService } from '.';

describe('FabricAuthRoleService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [FabricAuthRoleService, AccessControlConfigService],
      imports: [HttpClientTestingModule]
    });
  });

  it(
    'should be created',
    inject([FabricAuthRoleService], (service: FabricAuthRoleService) => {
      expect(service).toBeTruthy();
    })
  );
});
