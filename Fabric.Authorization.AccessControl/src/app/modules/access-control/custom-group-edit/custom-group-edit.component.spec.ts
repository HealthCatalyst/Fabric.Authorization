import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { CustomGroupEditComponent } from './custom-group-edit.component';

describe('CustomGroupEditComponent', () => {
  let component: CustomGroupEditComponent;
  let fixture: ComponentFixture<CustomGroupEditComponent>;

  beforeEach(
    async(() => {
      TestBed.configureTestingModule({
        declarations: [CustomGroupEditComponent]
      }).compileComponents();
    })
  );

  beforeEach(() => {
    fixture = TestBed.createComponent(CustomGroupEditComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
