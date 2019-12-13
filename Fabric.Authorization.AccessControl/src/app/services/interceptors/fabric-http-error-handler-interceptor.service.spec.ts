import { TestBed, inject } from '@angular/core/testing';

import { FabricHttpErrorHandlerInterceptorService } from './fabric-http-error-handler-interceptor.service';
import { AlertService } from '../global/alert.service';
import { ToasterModule } from '@healthcatalyst/cashmere';
import { OverlayModule } from '@angular/cdk/overlay';

describe('FabricHttpErrorHandlerInterceptorService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ToasterModule, OverlayModule],
      providers: [
        FabricHttpErrorHandlerInterceptorService,
        AlertService
      ]
    });
  });

  it(
    'should be created',
    inject(
      [FabricHttpErrorHandlerInterceptorService],
      (service: FabricHttpErrorHandlerInterceptorService) => {
        expect(service).toBeTruthy();
      }
    )
  );
});
