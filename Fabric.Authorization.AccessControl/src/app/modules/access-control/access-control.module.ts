import { NgModule, ModuleWithProviders } from '@angular/core';
import { CommonModule } from '@angular/common';

import { AccessControlRoutingModule } from './access-control-routing.module';
import { UserlistComponent } from './userlist/userlist.component';
import { UseraddComponent } from './useradd/useradd.component';
import { AuthserviceService } from '../../services/authservice.service';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { FabricAuthBaseService } from '../../services/fabric-auth-base.service';
import { FabricAuthGroupService } from '../../services/fabric-auth-group.service';
import { FabricAuthMemberSearchService } from '../../services/fabric-auth-member-search.service';
import { FabricAuthUserService } from '../../services/fabric-auth-user.service';
import { FabricExternalIdpSearchService } from '../../services/fabric-external-idp-search.service';
import { httpInterceptorProviders } from '../../services/interceptors';

@NgModule({
  imports: [
    CommonModule,
    AccessControlRoutingModule,
    HttpClientModule
  ],
  declarations: [UserlistComponent, UseraddComponent],  
  providers: [
    FabricAuthBaseService,
    FabricAuthGroupService,
    FabricAuthMemberSearchService,
    FabricAuthUserService,
    FabricExternalIdpSearchService,
    httpInterceptorProviders
  ],
  exports:[
    UserlistComponent,
    UseraddComponent
  ]
})
export class AccessControlModule { }
