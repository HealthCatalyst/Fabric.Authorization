import { TestBed, inject } from '@angular/core/testing';
import {
  HttpHandler,
  HttpClient } from '@angular/common/http';
import { FabricHttpRequestInterceptorService } from './fabric-http-request-interceptor.service';
import { AuthService } from '../global/auth.service';
import { ServicesService } from '../global/services.service';
import { ConfigService } from '../global/config.service';

describe('FabricHttpRequestInterceptorService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        FabricHttpRequestInterceptorService,
        HttpClient,
        HttpHandler,
        ServicesService,
        ConfigService,
        {
          provide: 'IAuthService',
          useClass: AuthService
        }
      ]
    });
  });

  it(
    'should be created',
    inject(
      [FabricHttpRequestInterceptorService],
      (service: FabricHttpRequestInterceptorService) => {
        expect(service).toBeTruthy();
      }
    )
  );
});
