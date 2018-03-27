import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { MemberAddComponent } from './member-add.component';

describe('MemberAddComponent', () => {
  let component: MemberAddComponent;
  let fixture: ComponentFixture<MemberAddComponent>;

  beforeEach(
    async(() => {
      TestBed.configureTestingModule({
        declarations: [MemberAddComponent]
      }).compileComponents();
    })
  );

  beforeEach(() => {
    fixture = TestBed.createComponent(MemberAddComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
