import { TestBed, inject } from '@angular/core/testing';

import { FabricHttpErrorHandlerInterceptorService } from './fabric-http-error-handler-interceptor.service';
import { AlertService } from '../global/alert.service';
import { ToastrService, ToastrModule } from 'ngx-toastr';

describe('FabricHttpErrorHandlerInterceptorService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ToastrModule.forRoot()],
      providers: [
        FabricHttpErrorHandlerInterceptorService,
        AlertService,
        ToastrService
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
