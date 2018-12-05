export class AlertServiceMock {
    showSyncWarning: jasmine.Spy = jasmine.createSpy('showSyncWarning');
    showError: jasmine.Spy = jasmine.createSpy('showError');
}
