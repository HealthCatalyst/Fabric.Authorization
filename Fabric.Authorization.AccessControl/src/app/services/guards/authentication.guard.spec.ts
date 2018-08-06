import { TestBed, async, inject } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';

import { AuthenticationGuard } from './authentication.guard';
import { AuthService } from '../global/auth.service';
import { ServicesService } from '../global/services.service';
import { ConfigService } from '../global/config.service';

describe('AuthenticationGuard', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AuthenticationGuard, AuthService, ServicesService, ConfigService]
    });
  });

  it('should ...', inject([AuthenticationGuard], (guard: AuthenticationGuard) => {
    expect(guard).toBeTruthy();
  }));
});
