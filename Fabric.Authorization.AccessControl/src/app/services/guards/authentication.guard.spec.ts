import { TestBed, async, inject } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';

import { AuthenticationGuard } from './authentication.guard';
import { AuthService } from '../global/auth.service';

describe('AuthenticationGuard', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AuthenticationGuard, AuthService]
    });
  });

  it('should ...', inject([AuthenticationGuard], (guard: AuthenticationGuard) => {
    expect(guard).toBeTruthy();
  }));
});
