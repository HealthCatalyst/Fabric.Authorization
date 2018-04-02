import { TestBed, inject } from '@angular/core/testing';

import { AuthService } from './auth.service';
import { HttpClientTestingModule } from '@angular/common/http/testing';

describe('AuthService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [AuthService],
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
