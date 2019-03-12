import { TestBed, inject, ComponentFixture } from '@angular/core/testing';

import { NameDisplayService } from './name-display.service';

describe('NameDisplayService', () => {
  let service: NameDisplayService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [NameDisplayService]
    });
  });

  beforeEach( inject([NameDisplayService], (nameService: NameDisplayService) => {
    service = nameService;
  }));

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getPrincipalNameToDisplay', () => {
    it('should not append tenant alias if principal is a user', () => {
      // arrange
      const principalName = 'azureuser@tenant.com';
      const principal = {
        subjectId: principalName,
        principalType: 'user',
        tenantId: 'tenantId',
        tenantAlias: 'tenantAlias',
        identityProviderUserPrincipalName: principalName,
      };

      // act
      const result = service.getPrincipalNameToDisplay(principal);

      // assert
      expect(result).toBe(principalName);
    });

    it('should not append tenant alias if principal is a group, and tenant alias is not present', () => {
      // arrange
      const principalName = 'azure group';
      const principal = {
        subjectId: principalName,
        principalType: 'group',
        tenantId: 'tenantId',
        identityProviderUserPrincipalName: principalName,
      };

      // act
      const result = service.getPrincipalNameToDisplay(principal);

      // assert
      expect(result).toBe(principalName);
    });

    it('should append tenant alias if principal is a group, and tenant alias is present', () => {
      // arrange
      const principalName = 'azure group';
      const tenantAlias = 'alias';
      const principal = {
        subjectId: principalName,
        principalType: 'group',
        tenantId: 'tenantId',
        tenantAlias: tenantAlias,
        identityProviderUserPrincipalName: principalName,
      };

      // act
      const result = service.getPrincipalNameToDisplay(principal);

      // assert
      expect(result).toBe(principalName + '@' + tenantAlias);
    });
  });

});
