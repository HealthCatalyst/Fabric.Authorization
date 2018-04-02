import { TestBed, inject } from '@angular/core/testing';

import { FabricBaseService } from '../services';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { AccessControlConfigService } from './access-control-config.service';

describe('FabricAuthBaseService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [FabricBaseService, AccessControlConfigService],
      imports: [HttpClientTestingModule]
    });
  });

  it(
    'should be created',
    inject([FabricBaseService], (service: FabricBaseService) => {
      expect(service).toBeTruthy();
    })
  );
});
