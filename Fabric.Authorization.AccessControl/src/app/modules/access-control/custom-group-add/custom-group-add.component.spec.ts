import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { CustomGroupAddComponent } from './custom-group-add.component';

describe('CustomGroupAddComponent', () => {
  let component: CustomGroupAddComponent;
  let fixture: ComponentFixture<CustomGroupAddComponent>;

  beforeEach(
    async(() => {
      TestBed.configureTestingModule({
        declarations: [CustomGroupAddComponent]
      }).compileComponents();
    })
  );

  beforeEach(() => {
    fixture = TestBed.createComponent(CustomGroupAddComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
