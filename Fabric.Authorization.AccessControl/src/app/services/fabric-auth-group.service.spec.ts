import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { Pipe, PipeTransform } from '@angular/core';
import { TestBed, inject, async } from '@angular/core/testing';
import { Observable } from 'rxjs/Rx';

import { FabricAuthGroupService } from '../services';
import { Group, User } from '../models';
import { ErrorObservable } from 'rxjs/observable/ErrorObservable';
import { httpInterceptorProviders } from './interceptors';

fdescribe('FabricAuthGroupService', () => {

  const mockGroupUserResponse = {
    groupName: 'Group 1',
    groupSource: 'Custom',
    users: [
      {
        name: 'First Last',
        subjectId: 'Sub123',
        identityProvider: 'Windows'
      }
    ]
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ HttpClientTestingModule ],
      providers: [ FabricAuthGroupService, httpInterceptorProviders ]
    });
  });

  it('should be created', inject([FabricAuthGroupService], (service: FabricAuthGroupService) => {
    expect(service).toBeTruthy();
  }));

  it('addUserToCustomGroup should deserialize all properties',
    async(  
      inject([HttpClient, HttpTestingController, FabricAuthGroupService], (
        httpClient: HttpClient,
        httpTestingController: HttpTestingController,
        service: FabricAuthGroupService) => {

        let user = new User('Windows', 'Sub123');
        user.name = 'First Last';

        // set up subscription to POST '/groups/Group 1/user' and service response expectations
        service.addUserToCustomGroup(mockGroupUserResponse.groupName, user).subscribe(returnedGroup => {
          expect(returnedGroup.groupName).toBe(mockGroupUserResponse.groupName);
          expect(returnedGroup.groupSource).toBe(mockGroupUserResponse.groupSource);
          expect(returnedGroup.users).toBeDefined();
          expect(returnedGroup.users.length).toEqual(1);
          let returnedUser = returnedGroup.users[0];
          let originalUser = mockGroupUserResponse.users[0];
          expect(returnedUser.subjectId).toEqual(originalUser.subjectId);
          expect(returnedUser.identityProvider).toEqual(originalUser.identityProvider);
          expect(returnedUser.name).toEqual(originalUser.name);
        });

        // simulate response from POST '/groups/Group 1/user' and flush response
        // to trigger subscription above
        const req = httpTestingController.expectOne(`${FabricAuthGroupService.baseGroupApiUrl}/${mockGroupUserResponse.groupName}/users`);
        expect(req.request.method).toBe("POST");
        req.flush(mockGroupUserResponse, {status: 201, statusText: 'Created'});
        
        httpTestingController.verify();
      })
    )
  );

  it('addUserToCustomGroup error should be caught',
    async(  
      inject([HttpClient, HttpTestingController, FabricAuthGroupService], (
        httpClient: HttpClient,
        httpTestingController: HttpTestingController,
        service: FabricAuthGroupService) => {

        let user = new User('Windows', 'Sub123');
        user.name = 'First Last';

        // set up subscription to POST '/groups/Group 1/user' and service response expectations
        service.addUserToCustomGroup(mockGroupUserResponse.groupName, user).catch(error => {
          expect(Observable.of(error)).toBeTruthy();
          console.log('caught error = ' + JSON.stringify(error));
          expect(error.statusCode).toBe(404);
          expect(error.message).toBe('Group not found');
          return Observable.of(error);
        })
        .subscribe();

        // simulate response from POST '/groups/Group 1/user' and flush response
        // to trigger subscription above
        const req = httpTestingController.expectOne(`${FabricAuthGroupService.baseGroupApiUrl}/${mockGroupUserResponse.groupName}/users`);
        expect(req.request.method).toBe("POST");
        req.flush({errorMessage: ''}, {status: 404, statusText: 'Group not found'});
        
        httpTestingController.verify();
      })
    )
  );

  it('removeUserFromCustomGroup should deserialize all properties',
    async(  
      inject([HttpClient, HttpTestingController, FabricAuthGroupService], (
        httpClient: HttpClient,
        httpTestingController: HttpTestingController,
        service: FabricAuthGroupService) => {

        let user = new User('Windows', 'Sub123');
        user.name = 'First Last';

        // set up subscription to POST '/groups/Group 1/user' and service response expectations
        service.removeUserFromCustomGroup(mockGroupUserResponse.groupName, user).subscribe(returnedGroup => {
          expect(returnedGroup.groupName).toBe(mockGroupUserResponse.groupName);
          expect(returnedGroup.groupSource).toBe(mockGroupUserResponse.groupSource);
          expect(returnedGroup.users).toBeDefined();
          expect(returnedGroup.users.length).toEqual(1);
          let returnedUser = returnedGroup.users[0];
          let originalUser = mockGroupUserResponse.users[0];
          expect(returnedUser.subjectId).toEqual(originalUser.subjectId);
          expect(returnedUser.identityProvider).toEqual(originalUser.identityProvider);
          expect(returnedUser.name).toEqual(originalUser.name);
        });

        // simulate response from POST '/groups/Group 1/user' and flush response
        // to trigger subscription above
        const req = httpTestingController.expectOne(`${FabricAuthGroupService.baseGroupApiUrl}/${mockGroupUserResponse.groupName}/users`);
        expect(req.request.method).toBe("DELETE");
      
        req.flush(mockGroupUserResponse, {status: 204, statusText: 'No Content'});
        
        httpTestingController.verify();
      })
    )
  );
});
