import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { OidccallbackComponent } from './oidccallback.component';

describe('OidccallbackComponent', () => {
  let component: OidccallbackComponent;
  let fixture: ComponentFixture<OidccallbackComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ OidccallbackComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(OidccallbackComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
