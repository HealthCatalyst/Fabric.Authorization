import { TestBed, inject } from '@angular/core/testing';
import {
  HttpInterceptor,
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpClient } from '@angular/common/http';
import { FabricHttpRequestInterceptorService } from './fabric-http-request-interceptor.service';
import { AuthService } from '../global/auth.service';

describe('FabricHttpRequestInterceptorService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [FabricHttpRequestInterceptorService, AuthService, HttpClient, HttpHandler]
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
