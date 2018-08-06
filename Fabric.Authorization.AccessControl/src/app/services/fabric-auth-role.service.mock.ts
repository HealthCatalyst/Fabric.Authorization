import { IRole } from '../models/role.model';

export const mockRoles: IRole[] = [
    { name: 'admin', grain: 'app', securableItem: 'foo' },
    { name: 'superuser', grain: 'app', securableItem: 'foo' }
];

export class FabricAuthRoleServiceMock {
    getRolesBySecurableItemAndGrain: jasmine.Spy = jasmine.createSpy('getRolesBySecurableItemAndGrain');
}
