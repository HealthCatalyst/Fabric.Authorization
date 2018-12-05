import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import {RouterTestingModule} from '@angular/router/testing';

import { NavbarModule, PopoverModule, IconModule } from '@healthcatalyst/cashmere';
import { NavbarComponent } from './navbar.component';
import { User } from 'oidc-client';
import { MockAuthService } from '../services/global/auth.service.mock';

describe('NavbarComponent', () => {
  let component: NavbarComponent;
  let fixture: ComponentFixture<NavbarComponent>;

  beforeEach(async(() => {
     TestBed.configureTestingModule({
       declarations: [ NavbarComponent ],
       imports: [RouterTestingModule, NavbarModule, PopoverModule, IconModule, HttpClientTestingModule],
       providers: [
         {
         provide: 'IAuthService',
         useClass: MockAuthService
        }
      ]
     })
     .compileComponents();
  }));

  beforeEach(() => {
     fixture = TestBed.createComponent(NavbarComponent);
     component = fixture.componentInstance;
     fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('getUserDisplayName() should return displayName for valid User with given_name and family_name', () => {
    const user = <User>{};
    user.profile = {family_name: 'user', given_name: 'test', name: 'domain\test.user'};
    expect(component.getUserDisplayName(user)).toBe('test user');
  });

  it('getUserDisplayName() should return displayName for valid User with name', () => {
    const user = <User>{};
    user.profile = {name: 'domain\test.user'};
    expect(component.getUserDisplayName(user)).toBe('domain\test.user');
  });

  it('getUserDisplayName() should return empty string for null User', () => {
    const user = null;
    expect(component.getUserDisplayName(user)).toBe('');
  });

  it('getUserDisplayName() should return empty string for null profile on User', () => {
    const user = <User>{};
    expect(component.getUserDisplayName(user)).toBe('');
  });
});
