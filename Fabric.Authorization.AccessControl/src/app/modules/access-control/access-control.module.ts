import { NgModule, ModuleWithProviders } from '@angular/core';
import { CommonModule } from '@angular/common';

import { AccessControlRoutingModule } from './access-control-routing.module';
import { UserlistComponent } from './userlist/userlist.component';
import { UseraddComponent } from './useradd/useradd.component';
import { AuthserviceService } from '../../services/authservice.service';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { FabricHttpInterceptorService } from '../../services/interceptors/fabric-http-interceptor.service';

@NgModule({
  imports: [
    CommonModule,
    AccessControlRoutingModule,
    HttpClientModule
  ],
  declarations: [UserlistComponent, UseraddComponent],  
  providers: [
    AuthserviceService,
    {
      provide: HTTP_INTERCEPTORS,
      useClass: FabricHttpInterceptorService,
      multi: true
    }
  ],
  exports:[
    UserlistComponent,
    UseraddComponent
  ]
})
export class AccessControlModule { }
