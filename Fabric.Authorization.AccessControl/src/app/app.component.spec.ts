import { TestBed, async } from '@angular/core/testing';
import { AppComponent } from './app.component';
import {RouterTestingModule} from '@angular/router/testing';
import { NavbarComponent } from './navbar/navbar.component';
import { NavbarModule, PopModule, IconModule, ModalModule, AppSwitcherModule } from '@healthcatalyst/cashmere';
import { AuthService } from './services/global/auth.service';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ServicesService } from './services/global/services.service';
import { ConfigService } from './services/global/config.service';
import { MockAppSwitcherConfig } from '../app/test/app-switcher-config.mock';

describe('AppComponent', () => {
  beforeEach(
    async(() => {
      TestBed.configureTestingModule({
        declarations: [AppComponent, NavbarComponent],
        imports: [RouterTestingModule, NavbarModule, PopModule, IconModule, ModalModule, AppSwitcherModule, HttpClientTestingModule],
        providers: [
          {
          provide: 'IAuthService',
          useClass: AuthService
          },
          ServicesService,
          ConfigService,
          MockAppSwitcherConfig]
      }).compileComponents();
    })
  );
  it(
    'should create the app',
    async(() => {
      const fixture = TestBed.createComponent(AppComponent);
      const app = fixture.debugElement.componentInstance;
      expect(app).toBeTruthy();
    })
  );
  it(
    `should have as title 'Fabric.Authorization.AccessControl Demo'`,
    async(() => {
      const fixture = TestBed.createComponent(AppComponent);
      const app = fixture.debugElement.componentInstance;
      expect(app.title).toEqual('Fabric.Authorization.AccessControl Demo');
    })
  );
});
