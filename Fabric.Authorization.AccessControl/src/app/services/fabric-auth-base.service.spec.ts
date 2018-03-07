import { TestBed, inject } from '@angular/core/testing';

import { FabricAuthBaseService } from '../services';

describe('FabricAuthBaseService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [FabricAuthBaseService]
    });
  });

  it('should be created', inject([FabricAuthBaseService], (service: FabricAuthBaseService) => {
    expect(service).toBeTruthy();
  }));
});
