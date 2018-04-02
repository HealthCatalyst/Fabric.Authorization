import { async, ComponentFixture, TestBed, inject } from '@angular/core/testing';

import { MemberListComponent } from './member-list.component';
import { ServicesMockModule } from '../services.mock.module';
import { PopoverModule, IconModule, ProgressIndicatorsModule } from '@healthcatalyst/cashmere';
import { FabricAuthMemberSearchServiceMock, mockAuthSearchResult } from '../../../services/fabric-auth-member-search.service.mock';
import { FabricAuthMemberSearchService } from '../../../services';
import { Observable } from 'rxjs/Observable';
import { FormsModule } from '@angular/forms';

describe('MemberListComponent', () => {
  let component: MemberListComponent;
  let fixture: ComponentFixture<MemberListComponent>;

  beforeEach(
    async(() => {
      TestBed.configureTestingModule({
        declarations: [MemberListComponent],
        imports: [FormsModule, ServicesMockModule, PopoverModule, IconModule, ProgressIndicatorsModule]
      }).compileComponents();
    })
  );

  beforeEach(inject([FabricAuthMemberSearchService], (memberSearchService: FabricAuthMemberSearchServiceMock) => {
    memberSearchService.searchMembers.and.returnValue(Observable.of(mockAuthSearchResult));
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(MemberListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
