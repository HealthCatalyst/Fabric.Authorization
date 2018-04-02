import { IAuthMemberSearchResult } from '../models';

export const mockAuthSearchResult: IAuthMemberSearchResult[] = [
    {
        subjectId: 'sub123',
        identityProvider: 'AD',
        firstName: 'First',
        middleName: 'Middle',
        lastName: 'Last',
        groupName: 'Group 1',
        roles: [
            { name: 'admin', grain: 'app', securableItem: 'foo' },
            { name: 'superuser', grain: 'app', securableItem: 'foo' }
        ],
        entityType: 'user'
    },
    {
        subjectId: 'sub345',
        identityProvider: 'AD',
        firstName: 'First2',
        middleName: 'Middle2',
        lastName: 'Last2',
        groupName: 'Group 2',
        roles: [{ name: 'viewer', grain: 'app', securableItem: 'foo' }],
        entityType: 'group'
    }
];

export class FabricAuthMemberSearchServiceMock {
    searchMembers: jasmine.Spy = jasmine.createSpy('searchMembers');
}
