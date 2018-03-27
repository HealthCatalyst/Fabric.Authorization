import { TestBed, inject } from '@angular/core/testing';

import { FabricBaseService } from '../services';

describe('FabricAuthBaseService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [FabricBaseService]
    });
  });

  it(
    'should be created',
    inject([FabricBaseService], (service: FabricBaseService) => {
      expect(service).toBeTruthy();
    })
  );
});
