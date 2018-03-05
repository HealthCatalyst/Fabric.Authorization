import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed, inject } from '@angular/core/testing';

import { FabricAuthGroupService } from './fabric-auth-group.service';
import { User } from '../models/User';

describe('FabricAuthGroupService', () => {
  let httpClient: HttpClient;
  let httpTestingController: HttpTestingController;
  let service: FabricAuthGroupService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ HttpClientTestingModule ],
      providers: [FabricAuthGroupService]
    });

    httpClient = TestBed.get(HttpClient);
    service = new FabricAuthGroupService(httpClient);
    httpTestingController = TestBed.get(HttpTestingController);
  });

  it('should be created', inject([FabricAuthGroupService], (service: FabricAuthGroupService) => {
    expect(service).toBeTruthy();
  }));

  it('should add user to custom group', () => {
    let mockApiResponse = new Response('');
    //spyOn(httpClient, 'get').and.returnValue();

    let user: User = new User('Windows', 'Sub123');
    user.name = 'First Last';
    expect(service.addUserToCustomGroup('Custom Group 1', user))
  });

  afterEach(() => {
    httpTestingController.verify();
  });
});
