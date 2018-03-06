import { HttpClient, HttpErrorResponse, HttpRequest } from '@angular/common/http';
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

  fit('should deserialize all group proper',
    async(  
      inject([HttpClient, HttpTestingController, FabricAuthGroupService], (
        httpClient: HttpClient,
        httpTestingController: HttpTestingController,
        service: FabricAuthGroupService) => {

        let group = new Group('Group 1', 'Custom');
        let user = new User('Windows', 'Sub123');
        user.name = 'First Last';
        group.users.push(user);

        service.addUserToCustomGroup(group.groupName, user).subscribe(returnedGroup => {
          console.log(returnedGroup);
          expect(returnedGroup.groupName).toBe(group.groupName);
          expect(returnedGroup.groupSource).toBe(group.groupSource);
          expect(returnedGroup.users).toBeDefined();
          expect(returnedGroup.users.length).toEqual(1);
          expect(returnedGroup.users[0]).toBe(user);
          expect(returnedGroup.users[0]).toEqual(user);
        });

        const req = httpTestingController.expectOne(`${FabricAuthGroupService.baseGroupApiUrl}/${group.groupName}/users`);
        expect(req.request.method).toBe("POST");
        req.flush(JSON.stringify(group));
      })
    )
  );
});
