export const mockCurrentUserPermissions: string[] = [
];

export class CurrentUserServiceMock {
    getPermissions: jasmine.Spy = jasmine.createSpy('getPermissions');
}
