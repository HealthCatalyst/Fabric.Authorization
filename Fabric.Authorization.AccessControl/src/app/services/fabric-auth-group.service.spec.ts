import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { Pipe, PipeTransform } from '@angular/core';
import { TestBed, inject, async } from '@angular/core/testing';
import { Observable } from 'rxjs/Rx';

import { FabricAuthGroupService } from './fabric-auth-group.service';
import { Group, User } from '../models';

describe('FabricAuthGroupService', () => {

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ HttpClientTestingModule ],
      providers: [ FabricAuthGroupService ]
    });
  });

  it('should be created', inject([FabricAuthGroupService], (service: FabricAuthGroupService) => {
    expect(service).toBeTruthy();
  }));

  fit('addUserToCustomGroup should deserialize all properties',
    async(  
      inject([HttpClient, HttpTestingController, FabricAuthGroupService], (
        httpClient: HttpClient,
        httpTestingController: HttpTestingController,
        service: FabricAuthGroupService) => {

        const mockResponse = {
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

        let user = new User('Windows', 'Sub123');
        user.name = 'First Last';

        // set up subscription to POST '/groups/Group 1/user' and service response expectations
        service.addUserToCustomGroup(mockResponse.groupName, user).subscribe(returnedGroup => {
          expect(returnedGroup.groupName).toBe(mockResponse.groupName);
          expect(returnedGroup.groupSource).toBe(mockResponse.groupSource);
          expect(returnedGroup.users).toBeDefined();
          expect(returnedGroup.users.length).toEqual(1);
          let returnedUser = returnedGroup.users[0];
          let originalUser = mockResponse.users[0];
          expect(returnedUser.subjectId).toEqual(originalUser.subjectId);
          expect(returnedUser.identityProvider).toEqual(originalUser.identityProvider);
          expect(returnedUser.name).toEqual(originalUser.name);
        });

        // simulate response from POST '/groups/Group 1/user' and flush response
        // to trigger subscription above
        const req = httpTestingController.expectOne(`${FabricAuthGroupService.baseGroupApiUrl}/${mockResponse.groupName}/users`);
        expect(req.request.method).toBe("POST");
        console.log(mockResponse);
        req.flush(mockResponse, {status: 201, statusText: 'Created'});
        
        httpTestingController.verify();
      })
    )
  );
});
