import { IdPSearchResult } from '../models/idpSearchResult.model';

export const mockExternalIdpSearchResult: IdPSearchResult = {
    resultCount: 2,
    principals: [
      {
        subjectId: 'sub123',
        firstName: 'First_1',
        middleName: '',
        lastName: 'Last_1',
        principalType: 'user',
        tenantId: null,
        identityProvider: 'Windows',
        identityProviderUserPrincipalName: 'sub123'
      },
      {
        subjectId: 'sub456',
        firstName: 'First_2',
        middleName: '',
        lastName: 'Last_2',
        principalType: 'user',
        tenantId: null,
        identityProvider: 'Windows',
        identityProviderUserPrincipalName: 'sub456'
      },
      {
        subjectId: 'azuresub123',
        firstName: 'azure_first_1',
        middleName: '',
        lastName: 'azure_last_2',
        externalIdentifier: 'external_id',
        tenantId: 'tenant_id',
        identityProvider: 'Azure',
        principalType: 'user',
        identityProviderUserPrincipalName: 'azuresub123'
      }
    ]
  };

export class FabricExternalIdpSearchServiceMock {
    search: jasmine.Spy = jasmine.createSpy('search');
}
