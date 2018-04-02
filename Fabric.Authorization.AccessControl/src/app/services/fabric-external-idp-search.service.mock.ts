import { IdPSearchResult } from '../models/idpSearchResult.model';
import { Observable } from 'rxjs/Observable';

export const mockExternalIdpSearchResult: IdPSearchResult = {
    resultCount: 2,
    principals: [
      {
        subjectId: 'sub123',
        firstName: 'First_1',
        middleName: '',
        lastName: 'Last_1',
        principalType: 'user'
      },
      {
        subjectId: 'sub456',
        firstName: 'First_2',
        middleName: '',
        lastName: 'Last_2',
        principalType: 'user'
      }
    ]
  };

export class FabricExternalIdpSearchServiceMock {
    searchUser: jasmine.Spy = jasmine.createSpy('searchUser');

    searchExternalIdP: jasmine.Spy = jasmine.createSpy('searchUser');
}
