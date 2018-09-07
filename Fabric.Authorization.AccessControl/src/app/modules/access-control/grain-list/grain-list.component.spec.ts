import { async, ComponentFixture, TestBed, inject } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';

import { MatTreeModule } from '@angular/material/tree';

import { ServicesMockModule } from '../services.mock.module';
import { GrainListComponent, GrainFlatNode, GrainNode } from './grain-list.component';
import { IGrain } from '../../../models/grain.model';
import { ISecurableItem } from '../../../models/securableItem.model';
import { MemberListComponent } from '../member-list/member-list.component';
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
  let testGrains: IGrain[];
  let testSecurableItems: ISecurableItem[];

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [GrainListComponent, MemberListComponent],
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
    testGrains = mockGrains;
    testSecurableItems = [
      { id: 'datamarts', name: 'datamarts', grain: 'dos', securableItems: null, clientOwner: '', createdBy: '', modifiedBy: '' }
    ];
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(GrainListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should create and select the first node by default', () => {
    // arrange
    const grains = testGrains;

    // act --the act of creating this object has called the constructor

    // assert
    expect(component.selectedNode).not.toEqual(null);
    expect(component.selectedNode.name).toBe(grains[0].securableItems[0].name);
  });

  describe('isSelectedNode', () => {

    it('should return empty string for comparing different nodes', () => {
      // Arrange
      const randomNode = new GrainFlatNode(null, 'test', null, 0, '');
      component.selectedNode = null;

      // Act
      const result = component.isSelectedNode(randomNode);

      // Assert
      expect(result).toBe('');
    });

    it('should return "selected" for comparing similar nodes', () => {
      // Arrange
      const randomNode = new GrainFlatNode(null, 'test', null, 0, '');
      component.selectedNode = randomNode;

      // Act
      const result = component.isSelectedNode(randomNode);

      // Assert
      expect(result).toBe('selected');
    });
  });

  describe('getIcon', () => {

    it('should return "fa-angle-right" if parent node is collapsed', () => {
      // Arrange
      const randomNode = new GrainFlatNode(true, 'test', null, 0, '');
      component.treeControl.dataNodes.push(randomNode);
      component.treeControl.collapse(randomNode);

      // Act
      const result = component.getIcon(randomNode);

      // Assert
      expect(result).toBe('fa-angle-right');
    });

    it('should return "fa-angle-down" if parent node is expanded', () => {
      // Arrange
      const randomNode = new GrainFlatNode(true, 'test', null, 0, '');
      component.treeControl.dataNodes.push(randomNode);
      component.treeControl.expand(randomNode);

      // Act
      const result = component.getIcon(randomNode);

      // Assert
      expect(result).toBe('fa-angle-down');
    });

    it('should return "fa-angle-down" if child node that cant expand', () => {
      // Arrange
      const randomNode = new GrainFlatNode(false, 'test', 'parent', 1, '');
      component.selectedNode = null;

      // Act
      const result = component.getIcon(randomNode);

      // Assert
      expect(result).toBe('fa-angle-down');
    });

    it('should return "fa-angle-right" if selected', () => {
      // Arrange
      const randomNode = new GrainFlatNode(false, 'test', 'parent', 1, '');
      component.selectedNode = randomNode;

      // Act
      const result = component.getIcon(randomNode);

      // Assert
      expect(result).toBe('fa-angle-right');
    });
  });

  describe('AddGrainToGrainNode', () => {

    it('given a IGrain[], convert into GrainNode[]', () => {
      // Arrange
      const grains = testGrains;

      // Act
      const result = component.addGrainToGrainNode(grains);

      // Assert
      expect(grains.length).toBe(result.length);

      for (let i = 0; i < grains.length; i++) {
        expect(grains[0].name).toBe(result[0].name);
      }
    });
  });

  describe('AddSecurableItemToGrainNode', () => {
    it('given a ISecurableItem[], convert into GrainNode[]', () => {
      // Arrange
      const parentName = 'dos';
      const securableItems = testSecurableItems;

      // Act
      const result = component.addSecurableItemToGrainNode(securableItems, parentName);

      // Assert
      expect(securableItems.length).toBe(result.length);

      for (let i = 0; i < securableItems.length; i++) {
        expect(securableItems[0].name).toBe(result[0].name);
        expect(parentName).toBe(result[0].parentName);
      }
    });
  });

  it('onSelect sets the selectedNode', () => {
    // Arrange
    const randomNode = new GrainFlatNode(true, 'test', 'parent', 0, '');
    component.selectedNode = null;

    // Act
    component.onSelect(randomNode);

    // Assert
    expect(component.selectedNode).toBe(randomNode);
  });
});
