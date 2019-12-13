import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { ButtonModule, ProgressIndicatorsModule, NavbarModule, PopModule,
  AppSwitcherModule, IconModule, ModalModule } from '@healthcatalyst/cashmere';

import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { NoCookiesComponent } from './no-cookies/no-cookies.component';
import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';

@NgModule({
  imports: [
    RouterModule.forRoot([])
  ],
  exports: [RouterModule]
})
export class NoCookiesAppRoutingModule {}

@NgModule({
  declarations: [NoCookiesComponent],
  imports: [NoCookiesAppRoutingModule, BrowserModule, BrowserAnimationsModule,
    ButtonModule, ProgressIndicatorsModule, NavbarModule, PopModule,
    AppSwitcherModule, IconModule, ModalModule],
  bootstrap: [NoCookiesComponent]
})
export class NoCookiesAppModule {}

export function bootstrapNoCookiesAppModule() {
  platformBrowserDynamic()
    .bootstrapModule(NoCookiesAppModule)
    .catch(err => console.log(err));
}
