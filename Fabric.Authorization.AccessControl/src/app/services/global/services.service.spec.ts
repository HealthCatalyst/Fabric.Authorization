import { TestBed, inject } from '@angular/core/testing';

import { ServicesService } from './services.service';
import { HttpClientTestingModule } from '../../../../node_modules/@angular/common/http/testing';
import { ConfigService } from './config.service';

describe('ServicesService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ServicesService, ConfigService],
      imports: [HttpClientTestingModule]
    });
  });

  it('should be created', inject([ServicesService], (service: ServicesService) => {
    expect(service).toBeTruthy();
  }));
});
