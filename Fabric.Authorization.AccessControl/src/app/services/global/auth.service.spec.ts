import { TestBed, inject } from '@angular/core/testing';

import { AuthService } from './auth.service';
import { ServicesService } from '../global/services.service';
import { ConfigService } from '../global/config.service';
import { HttpClientTestingModule } from '@angular/common/http/testing';

describe('AuthService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [AuthService, ServicesService, ConfigService],
      imports: [HttpClientTestingModule]
    });
  });

  it(
    'should create',
    inject([AuthService], (service: AuthService) => {
      expect(service).toBeTruthy();
    })
  );
});
