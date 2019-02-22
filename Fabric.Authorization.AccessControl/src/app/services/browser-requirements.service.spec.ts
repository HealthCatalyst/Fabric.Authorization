import { TestBed, inject } from '@angular/core/testing';

import { BrowserRequirementsService } from './browser-requirements.service';

describe('BrowserRequirementsService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [BrowserRequirementsService]
    });
  });

  it('should be created', inject([BrowserRequirementsService], (service: BrowserRequirementsService) => {
    expect(service).toBeTruthy();
  }));

  it('should return true', inject([BrowserRequirementsService], (service: BrowserRequirementsService) => {
    expect(service.cookiesEnabled()).toBeTruthy();
  }));
});
