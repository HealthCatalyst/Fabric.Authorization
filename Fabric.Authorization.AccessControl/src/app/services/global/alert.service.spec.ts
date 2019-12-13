import { TestBed, inject } from '@angular/core/testing';

import { ToasterModule } from '@healthcatalyst/cashmere';
import { OverlayModule } from '@angular/cdk/overlay';
import { AlertService } from './alert.service';

describe('AlertService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ToasterModule,OverlayModule],
      providers: [AlertService]
    });
  });

  it('should be created', inject([AlertService], (service: AlertService) => {
    expect(service).toBeTruthy();
  }));
});
