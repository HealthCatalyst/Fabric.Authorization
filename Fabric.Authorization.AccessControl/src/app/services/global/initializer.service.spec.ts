import { TestBed, inject } from '@angular/core/testing';

import { InitializerService } from './initializer.service';
import { IAuthService } from './auth.service';
import { MockAuthService } from './auth.service.mock';
import { ServicesService } from './services.service';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ConfigService } from './config.service';

describe('InitializerService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [InitializerService,
        ServicesService,
        ConfigService,
        {
          provide: 'IAuthService',
          useClass: MockAuthService
        }
      ]
    });
  });

  it('should be created', inject([InitializerService], (service: InitializerService) => {
    expect(service).toBeTruthy();
  }));
});
