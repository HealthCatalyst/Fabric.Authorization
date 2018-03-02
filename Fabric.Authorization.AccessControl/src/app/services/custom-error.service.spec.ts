import { TestBed, inject } from '@angular/core/testing';

import { CustomErrorService } from './custom-error.service';

describe('CustomErrorService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [CustomErrorService]
    });
  });

  it('should ...', inject([CustomErrorService], (service: CustomErrorService) => {
    expect(service).toBeTruthy();
  }));
});
