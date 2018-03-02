import { TestBed, inject } from '@angular/core/testing';

import { FabricAuthMemberService } from './fabric-auth-member.service';

describe('FabricAuthMemberService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [FabricAuthMemberService]
    });
  });

  it('should be created', inject([FabricAuthMemberService], (service: FabricAuthMemberService) => {
    expect(service).toBeTruthy();
  }));
});
