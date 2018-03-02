import { NgModule, ModuleWithProviders } from '@angular/core';
import { CommonModule } from '@angular/common';

import { AccessControlRoutingModule } from './access-control-routing.module';
import { UserlistComponent } from './userlist/userlist.component';
import { UseraddComponent } from './useradd/useradd.component';
import { AuthserviceService } from '../../services/authservice.service';

@NgModule({
  imports: [
    CommonModule,
    AccessControlRoutingModule
  ],
  declarations: [UserlistComponent, UseraddComponent],  
  providers: [
    AuthserviceService
  ],
  exports:[
    UserlistComponent,
    UseraddComponent
  ]
})
export class AccessControlModule { }
