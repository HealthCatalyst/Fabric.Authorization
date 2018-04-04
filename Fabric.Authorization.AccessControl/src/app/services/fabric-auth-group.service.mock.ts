import { IUser } from '../models/user.model';
import { IRole } from '../models/role.model';
import { IGroup } from '../models/group.model';
import { Observable } from 'rxjs/Observable';

const groupName = 'DosAdminGroup';
const groupSource = 'Custom';
const dosAdminRoleDisplayName = 'DOS Administrators (role)';
const dosAdminRoleDescription = 'Administers DOS items (role)';

const dosSuperUsersRoleDisplayName = 'DOS Super Users (role)';
const dosSuperUsersRoleDescription = 'Elevated DOS privileges (role)';

const dosAdminGroupDisplayName = 'DOS Administrators (group)';
const dosAdminGroupDescription = 'Administers DOS items (group)';

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
        parentRole: 'admin_parent',
        displayName: dosAdminRoleDisplayName,
        description: dosAdminRoleDescription,
    },
    {
        name: 'superuser',
        grain: 'dos',
        securableItem: 'datamart',
        childRoles: ['dos_child1', 'dos_child2'],
        displayName: dosSuperUsersRoleDisplayName,
        description: dosSuperUsersRoleDescription,
    }
];

export const mockGroupResponse: IGroup = {
    groupName: groupName,
    groupSource: groupSource,
    users: mockUsersResponse,
    roles: mockRolesResponse,
    displayName: dosAdminGroupDisplayName,
    description: dosAdminGroupDescription,
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
