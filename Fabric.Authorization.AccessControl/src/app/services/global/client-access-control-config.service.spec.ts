import { TestBed, inject } from '@angular/core/testing';

import { ClientAccessControlConfigService } from './client-access-control-config.service';
import { AuthService } from './auth.service';
import { HttpClientTestingModule } from '@angular/common/http/testing';

describe('ClientAccessControlConfigService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ClientAccessControlConfigService, AuthService],
      imports: [HttpClientTestingModule]
    });
  });

  it(
    'should be created',
    inject(
      [ClientAccessControlConfigService],
      (service: ClientAccessControlConfigService) => {
        expect(service).toBeTruthy();
      }
    )
  );
});
