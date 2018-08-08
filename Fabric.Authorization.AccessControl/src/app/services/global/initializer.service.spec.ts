import { TestBed, inject } from '@angular/core/testing';

import { InitializerService } from './initializer.service';
import { IAuthService } from './auth.service';
import { MockAuthService } from './auth.service.mock';

describe('InitializerService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [InitializerService, {provide: 'IAuthService', useClass: MockAuthService }]
    });
  });

  it('should be created', inject([InitializerService], (service: InitializerService) => {
    expect(service).toBeTruthy();
  }));
});
