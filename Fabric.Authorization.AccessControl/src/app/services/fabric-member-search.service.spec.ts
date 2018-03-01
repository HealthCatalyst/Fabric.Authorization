import { TestBed, inject } from '@angular/core/testing';

import { FabricMemberSearchService } from './fabric-member-search.service';

describe('FabricMemberSearchService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [FabricMemberSearchService]
    });
  });

  it('should be created', inject([FabricMemberSearchService], (service: FabricMemberSearchService) => {
    expect(service).toBeTruthy();
  }));
});
