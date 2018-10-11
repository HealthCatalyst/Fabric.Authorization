import { Observable, of } from 'rxjs';
import { async, ComponentFixture, TestBed, inject } from '@angular/core/testing';
import { Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';

import { MemberListComponent } from './member-list.component';
import { ServicesMockModule } from '../services.mock.module';
import { PopoverModule, IconModule, ProgressIndicatorsModule, SelectModule, ModalModule, PaginationModule } from '@healthcatalyst/cashmere';
import { FabricAuthMemberSearchServiceMock, mockAuthSearchResult } from '../../../services/fabric-auth-member-search.service.mock';
import { FormsModule } from '@angular/forms';
import { FabricAuthMemberSearchService } from '../../../services/fabric-auth-member-search.service';
import { IAuthMemberSearchResult } from '../../../models/authMemberSearchResult.model';

describe('MemberListComponent', () => {
  let component: MemberListComponent;
  let fixture: ComponentFixture<MemberListComponent>;

  beforeEach(
    async(() => {
      TestBed.configureTestingModule({
        declarations: [MemberListComponent],
        imports: [
          FormsModule,
          ServicesMockModule,
          IconModule,
          ModalModule,
          PaginationModule,
          PopoverModule,
          ProgressIndicatorsModule,
          SelectModule,
          RouterTestingModule,
        ]
      }).compileComponents();
    })
  );

  beforeEach(inject([FabricAuthMemberSearchService], (memberSearchService: FabricAuthMemberSearchServiceMock) => {
    memberSearchService.searchMembers.and.returnValue(of(mockAuthSearchResult));
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(MemberListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('getMemberNameToDisplay', () => {

    it('should return the display name if exist', () => {
      // Arrange
      const displayName = 'displayName';
      const member = {
        displayName: displayName,
        groupName: 'groupName',
        subjectId: '1',
        identityProvider: 'test',
        roles: null,
        firstName: 'test',
        middleName: 'test',
        lastName: 'test',
        lastLoginDateTimeUtc: null,
        entityType: null,
        name: null
      };
      // Act
      const result = component.getMemberNameToDisplay(member);

      // Assert
      expect(result).toBe(displayName);
    });

    it('should return the group name if exist and display name is null', () => {
      // Arrange
      const groupName = 'groupName';
      const member = {
        displayName: null,
        groupName: groupName,
        subjectId: '1',
        identityProvider: 'test',
        roles: null,
        firstName: 'test',
        middleName: 'test',
        lastName: 'test',
        lastLoginDateTimeUtc: null,
        entityType: null,
        name: null
      };
      // Act
      const result = component.getMemberNameToDisplay(member);

      // Assert
      expect(result).toBe(groupName);
    });

    it('should return the group name if exist and display name is undefined', () => {
      // Arrange
      const groupName = 'groupName';
      const member = {
        displayName: undefined,
        groupName: groupName,
        subjectId: '1',
        identityProvider: 'test',
        roles: null,
        firstName: 'test',
        middleName: 'test',
        lastName: 'test',
        lastLoginDateTimeUtc: null,
        entityType: null,
        name: null
      };
      // Act
      const result = component.getMemberNameToDisplay(member);

      // Assert
      expect(result).toBe(groupName);
    });

    it('should return the group name if exist and display name is empty string', () => {
      // Arrange
      const groupName = '';
      const member = {
        displayName: undefined,
        groupName: groupName,
        subjectId: '1',
        identityProvider: 'test',
        roles: null,
        firstName: 'test',
        middleName: 'test',
        lastName: 'test',
        lastLoginDateTimeUtc: null,
        entityType: null,
        name: null
      };
      // Act
      const result = component.getMemberNameToDisplay(member);

      // Assert
      expect(result).toBe(groupName);
    });
  });
});
