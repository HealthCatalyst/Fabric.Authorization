import { TestBed, async } from '@angular/core/testing';
import { AppComponent } from './app.component';
import {RouterTestingModule} from '@angular/router/testing';
import { NavbarComponent } from './navbar/navbar.component';
import { NavbarModule, PopoverModule, IconModule } from '@healthcatalyst/cashmere';
import { AuthService } from './services/global/auth.service';
import { HttpClientTestingModule } from '@angular/common/http/testing';

describe('AppComponent', () => {
  beforeEach(
    async(() => {
      TestBed.configureTestingModule({
        declarations: [AppComponent, NavbarComponent],
        imports: [RouterTestingModule, NavbarModule, PopoverModule, IconModule, HttpClientTestingModule],
        providers: [AuthService]
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
