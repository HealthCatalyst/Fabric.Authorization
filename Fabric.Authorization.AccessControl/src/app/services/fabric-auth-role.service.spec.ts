import { TestBed, inject } from '@angular/core/testing';

import { FabricAuthRoleService } from './fabric-auth-role.service';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { MockAccessControlConfigService } from './access-control-config.service.mock';

describe('FabricAuthRoleService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [FabricAuthRoleService,
        {
          provide: 'IAccessControlConfigService',
          useClass: MockAccessControlConfigService
      }],
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
