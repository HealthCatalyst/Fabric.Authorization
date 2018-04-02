import { IUser } from '../models/user.model';
import { IRole } from '../models/role.model';
import { IGroup } from '../models/group.model';
import { Observable } from 'rxjs/Observable';

const groupName = 'Dos Admin Group';
const groupSource = 'Custom';

export const mockUsersResponse: IUser[] = [
    {
        name: 'First Last',
        subjectId: 'Sub123',
        identityProvider: 'Windows'
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

export const mockGroupResponse: IGroup = {
    groupName: groupName,
    groupSource: groupSource,
    users: mockUsersResponse,
    roles: mockRolesResponse
};

export class FabricAuthGroupServiceMock {
    getGroup: jasmine.Spy = jasmine.createSpy('getGroup');

    getGroupUsers: jasmine.Spy = jasmine.createSpy('getGroupUsers');

    addUsersToCustomGroup: jasmine.Spy = jasmine.createSpy('addUsersToCustomGroup');

    removeUserFromCustomGroup: jasmine.Spy = jasmine.createSpy('removeUserFromCustomGroup');

    getGroupRoles: jasmine.Spy = jasmine.createSpy('getGroupRoles');

    addRolesToGroup: jasmine.Spy = jasmine.createSpy('addRolesToGroup');

    removeRolesFromGroup: jasmine.Spy = jasmine.createSpy('removeRolesFromGroup');

    createGroup: jasmine.Spy = jasmine.createSpy('createGroup');
}
