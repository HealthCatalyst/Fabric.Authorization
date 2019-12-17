import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import {RouterTestingModule} from '@angular/router/testing';
import { NavbarModule, PopModule, IconModule, ModalModule, AppSwitcherModule } from '@healthcatalyst/cashmere';
import { MockAppSwitcherConfig } from '../test/app-switcher-config.mock';
import { NoCookiesComponent } from './no-cookies.component';

describe('NoCookiesComponent', () => {
  let component: NoCookiesComponent;
  let fixture: ComponentFixture<NoCookiesComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ NoCookiesComponent ],
      imports: [RouterTestingModule, NavbarModule, PopModule, IconModule, ModalModule, AppSwitcherModule],
      providers: [MockAppSwitcherConfig]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(NoCookiesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
