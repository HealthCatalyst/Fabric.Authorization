import { TestBed, inject } from '@angular/core/testing';

import { AccessControlConfigService } from '../services';

describe('AccessControlConfigService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [AccessControlConfigService]
    });
  });

  it('should be created', inject([AccessControlConfigService], (service: AccessControlConfigService) => {
    expect(service).toBeTruthy();
  }));
});
