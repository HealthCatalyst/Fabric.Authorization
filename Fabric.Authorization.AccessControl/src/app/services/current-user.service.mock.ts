export const mockCurrentUserPermissions: string[] = [
];

export const mockAdminUserPermissions: string[] = [
    'dos/datamarts.manageauthorization'
];

export class CurrentUserServiceMock {
    getPermissions: jasmine.Spy = jasmine.createSpy('getPermissions');
}
