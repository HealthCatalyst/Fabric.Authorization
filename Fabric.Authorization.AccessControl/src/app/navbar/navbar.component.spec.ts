import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import {RouterTestingModule} from '@angular/router/testing';

import { NavbarModule, PopModule, IconModule, ModalModule, AppSwitcherModule } from '@healthcatalyst/cashmere';
import { NavbarComponent } from './navbar.component';
import { User } from 'oidc-client';
import { MockAuthService } from '../services/global/auth.service.mock';
import { MockAppSwitcherConfig } from '../test/app-switcher-config.mock';

describe('NavbarComponent', () => {
  let component: NavbarComponent;
  let fixture: ComponentFixture<NavbarComponent>;

  beforeEach(async(() => {
     TestBed.configureTestingModule({
       declarations: [ NavbarComponent ],
       imports: [RouterTestingModule, NavbarModule, PopModule, IconModule, ModalModule, AppSwitcherModule, HttpClientTestingModule],
       providers: [
         {
          provide: 'IAuthService',
          useClass: MockAuthService
         },
         MockAppSwitcherConfig
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

  describe('getUserDisplayName for Windows AD', () => {
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

  describe('getUserDisplayName for Azure AD', () => {
    it('getUserDisplayName() should return name if profile.name is not an array', () => {
      const user = <User>{};
      user.profile = { idp: 'AzureActiveDirectory', name: 'test user' };
      expect(component.getUserDisplayName(user)).toBe('test user');
    });

    it('getUserDisplayName() should return name at 0 if profile.name is an array of 1', () => {
      const user = <User>{};
      user.profile = { idp: 'AzureActiveDirectory', name: ['test user'] };
      expect(component.getUserDisplayName(user)).toBe('test user');
    });

    it('getUserDisplayName() should return index 1 name, and not user principle', () => {
      const user = <User>{};
      user.profile = { idp: 'AzureActiveDirectory', name: ['user@test', 'test user'] };
      expect(component.getUserDisplayName(user)).toBe('test user');
    });

    it('getUserDisplayName() should return user principle', () => {
      const user = <User>{};
      user.profile = { idp: 'AzureActiveDirectory', name: ['user@test'] };
      expect(component.getUserDisplayName(user)).toBe('user@test');
    });

    it('getUserDisplayName() no name property returns "no name detected"', () => {
      const user = <User>{};
      user.profile = { idp: 'AzureActiveDirectory' };
      expect(component.getUserDisplayName(user)).toBe('no name detected');
    });

    it('getUserDisplayName() null name property returns "no name detected"', () => {
      const user = <User>{};
      user.profile = { idp: 'AzureActiveDirectory', name: null };
      expect(component.getUserDisplayName(user)).toBe('no name detected');
    });
  });
});
