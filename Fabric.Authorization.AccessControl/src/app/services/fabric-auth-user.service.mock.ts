import { IGroup } from '../models/group.model';
import { IRole } from '../models/role.model';
import { IUser } from '../models/user.model';
import { IUserPermissionResponse } from '../models/userPermissionResponse.model';

const idP = 'ad';
const subjectId = 'sub123';

export const mockGroupsResponse: IGroup[] = [
    {
        groupName: 'Group 1',
        groupSource: idP
    },
    {
        groupName: 'Group 2',
        groupSource: idP
    }
];

export const mockRolesResponse: IRole[] = [
    {
        name: 'admin',
        grain: 'dos',
        securableItem: 'datamart',
        parentRole: 'admin_parent'
    },
    {
        name: 'superuser',
        grain: 'dos',
        securableItem: 'datamart',
        childRoles: ['dos_child1', 'dos_child2']
    }
];

export const mockUserResponse: IUser = {
    id: idP,
    name: 'First Last',
    identityProvider: idP,
    subjectId: subjectId,
    groups: mockGroupsResponse,
    roles: mockRolesResponse
};

export const mockUserPermissionResponse: IUserPermissionResponse = {
    permissions: [
    ],
    permissionRequestContexts: [
    ]
};

export class FabricAuthUserServiceMock {
    getUser: jasmine.Spy = jasmine.createSpy('getUser');

    getUserGroups: jasmine.Spy = jasmine.createSpy('getUserGroups');

    getUserRoles: jasmine.Spy = jasmine.createSpy('getUserRoles');

    addRolesToUser: jasmine.Spy = jasmine.createSpy('addRolesToUser');

    removeRolesFromUser: jasmine.Spy = jasmine.createSpy('removeRolesFromUser');

    createUser: jasmine.Spy = jasmine.createSpy('createUser');

    getCurrentUserPermissions: jasmine.Spy = jasmine.createSpy('getCurrentUserPermissions');
}
