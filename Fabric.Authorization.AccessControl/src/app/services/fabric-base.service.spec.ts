import { TestBed, inject } from '@angular/core/testing';

import { HttpClientTestingModule } from '@angular/common/http/testing';
import { FabricBaseService } from './fabric-base.service';
import { MockAccessControlConfigService } from './access-control-config.service.mock';

describe('FabricAuthBaseService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [FabricBaseService, {
        provide: 'IAccessControlConfigService',
        useClass: MockAccessControlConfigService
      }],
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
