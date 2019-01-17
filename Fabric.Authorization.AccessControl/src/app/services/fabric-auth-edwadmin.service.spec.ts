import {
  HTTP_INTERCEPTORS
} from '@angular/common/http';
import {
  HttpClientTestingModule, HttpTestingController
} from '@angular/common/http/testing';
import { FabricHttpErrorHandlerInterceptorService } from './interceptors/fabric-http-error-handler-interceptor.service';
import { TestBed } from '@angular/core/testing';

import { FabricAuthEdwAdminService } from './fabric-auth-edwadmin.service';
import { MockAccessControlConfigService } from './access-control-config.service.mock';
import { AlertService } from './global/alert.service';
import { ToastrModule } from 'ngx-toastr';

describe('FabricAuthEdwadminService', () => {
  let httpTestingController: HttpTestingController;
  let edwService: FabricAuthEdwAdminService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule, ToastrModule.forRoot()],
      providers: [FabricAuthEdwAdminService,
        AlertService,
        {
          provide: HTTP_INTERCEPTORS,
          useClass: FabricHttpErrorHandlerInterceptorService,
          multi: true
        },
        {
          provide: 'IAccessControlConfigService',
          useClass: MockAccessControlConfigService
        }]
    });
      httpTestingController = TestBed.get(HttpTestingController);
      edwService = TestBed.get(FabricAuthEdwAdminService);
  });

  it('should post a group with no query string', () => {
    // arrange
    const groupName = 'dummygroup';

    // act
    edwService.syncGroupWithEdwAdmin(groupName).subscribe();
    // assert
    const req = httpTestingController.expectOne(`auth/edw/${encodeURI(groupName)}/roles`);
    expect(req.request.method).toBe('POST');
    req.flush({});
    httpTestingController.verify();
  });

  it('should post a group with query string when idprovider and tenant provided', () => {
    // arrange
    const groupName = 'dummygroup';
    const idp = 'windows';
    const tenant = 'sample tenant';

    // act
    edwService.syncGroupWithEdwAdmin(groupName, idp, tenant).subscribe();

    // assert
    const req = httpTestingController.expectOne(r => r.url.includes(`auth/edw/${encodeURI(groupName)}/roles`));
    expect(req.request.method).toBe('POST');
    expect(req.request.params.has('tenantId')).toBe(true);
    expect(req.request.params.get('tenantId')).toEqual(tenant);
    expect(req.request.params.has('identityProvider')).toBe(true);
    expect(req.request.params.get('identityProvider')).toEqual(idp);
    req.flush({});
    httpTestingController.verify();
  });

  it('should post an array of users when syncing', () => {
    // arrange
    const users = [];
    users.push({subjectId: 'user1', identityProvider: 'windows'});
    users.push({subjectId: 'user2', identityProvider: 'windows'});

    // act
    edwService.syncUsersWithEdwAdmin(users).subscribe();

    // Assert
    const req = httpTestingController.expectOne(`auth/edw/roles`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(users);

    req.flush({});
    httpTestingController.verify();
  });
});
