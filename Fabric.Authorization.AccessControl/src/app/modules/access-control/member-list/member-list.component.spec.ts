import { async, ComponentFixture, TestBed, inject } from '@angular/core/testing';

import { MemberListComponent } from './member-list.component';
import { ServicesMockModule } from '../services.mock.module';
import { PopoverModule, IconModule, ProgressIndicatorsModule, SelectModule, ModalModule, PaginationModule } from '@healthcatalyst/cashmere';
import { FabricAuthMemberSearchServiceMock, mockAuthSearchResult } from '../../../services/fabric-auth-member-search.service.mock';
import { Observable } from 'rxjs/Observable';
import { FormsModule } from '@angular/forms';
import { FabricAuthMemberSearchService } from '../../../services/fabric-auth-member-search.service';

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
        ]
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
