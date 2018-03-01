import { TestBed, inject } from '@angular/core/testing';

import { FabricExternalIdpSearchService } from './fabric-external-idp-search.service';

describe('FabricExternalIdpSearchService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [FabricExternalIdpSearchService]
    });
  });

  it('should be created', inject([FabricExternalIdpSearchService], (service: FabricExternalIdpSearchService) => {
    expect(service).toBeTruthy();
  }));
});
