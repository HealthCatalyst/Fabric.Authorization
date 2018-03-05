import { TestBed, inject } from '@angular/core/testing';

import { FabricAuthMemberSearchService } from './fabric-auth-member-search.service';

describe('FabricAuthMemberSearchService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [FabricAuthMemberSearchService]
    });
  });

  it('should be created', inject([FabricAuthMemberSearchService], (service: FabricAuthMemberSearchService) => {
    expect(service).toBeTruthy();
  }));
});
