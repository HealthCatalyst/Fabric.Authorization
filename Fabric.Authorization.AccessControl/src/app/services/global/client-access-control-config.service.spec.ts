import { TestBed, inject } from '@angular/core/testing';

import { ClientAccessControlConfigService } from './client-access-control-config.service';

describe('ClientAccessControlConfigService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ClientAccessControlConfigService]
    });
  });

  it('should be created', inject([ClientAccessControlConfigService], (service: ClientAccessControlConfigService) => {
    expect(service).toBeTruthy();
  }));
});
