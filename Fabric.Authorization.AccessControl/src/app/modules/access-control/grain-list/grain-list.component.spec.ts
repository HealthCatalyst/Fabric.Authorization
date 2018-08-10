import { async, ComponentFixture, TestBed, inject } from '@angular/core/testing';
import { Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';

import { MatTreeFlatDataSource, MatTreeFlattener, MatTreeModule } from '@angular/material/tree';
import { FlatTreeControl } from '@angular/cdk/tree';

import { ServicesMockModule } from '../services.mock.module';
import { GrainListComponent } from './grain-list.component';
import { MemberListComponent } from '../member-list/member-list.component'
import { Observable } from 'rxjs/Observable';

import { FabricAuthGrainServiceMock, mockGrains } from '../../../services/fabric-auth-grain.service.mock';
import { FabricAuthGrainService } from '../../../services/fabric-auth-grain.service';

import { PopoverModule, IconModule, ProgressIndicatorsModule, SelectModule, ModalModule, PaginationModule } from '@healthcatalyst/cashmere';
import { FabricAuthMemberSearchServiceMock, mockAuthSearchResult } from '../../../services/fabric-auth-member-search.service.mock';
import { FormsModule } from '@angular/forms';
import { FabricAuthMemberSearchService } from '../../../services/fabric-auth-member-search.service';

describe('GrainListComponent', () => {
  let component: GrainListComponent;
  let fixture: ComponentFixture<GrainListComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ GrainListComponent, MemberListComponent ],
      imports: [ServicesMockModule, 
        MatTreeModule,
        FormsModule,
        IconModule,
        ModalModule,
        PaginationModule,
        PopoverModule,
        ProgressIndicatorsModule,
        SelectModule,
        RouterTestingModule]
    })
    .compileComponents();
  }));

  beforeEach(inject([FabricAuthGrainService], (grainService: FabricAuthGrainServiceMock) => {
    grainService.getAllGrains.and.returnValue((Observable.of(mockGrains)));
    grainService.isGrainVisible.and.returnValue(true);
  }));

  beforeEach(inject([FabricAuthMemberSearchService], (memberSearchService: FabricAuthMemberSearchServiceMock) => {
    memberSearchService.searchMembers.and.returnValue(Observable.of(mockAuthSearchResult));
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(GrainListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render all grains and securable Items', () => {

  });

  it('should collapse a node in a tree', () => {

  });

  it('should expand a node in a tree', () => {

  });

  it('should set the proper values when a grain/securable item is selected', () => {
    // write a test in member list to verify you can "switch" these values too

    // also write a test where a user does not have access in member list
  });

  it('should be able to deep link', () => {

  });
});
