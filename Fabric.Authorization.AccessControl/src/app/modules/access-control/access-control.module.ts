import { NgModule, ModuleWithProviders } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';

import { AccessControlRoutingModule } from './access-control-routing.module';
import { FabricAuthBaseService } from '../../services/fabric-auth-base.service';
import { FabricAuthGroupService } from '../../services/fabric-auth-group.service';
import { FabricAuthMemberSearchService } from '../../services/fabric-auth-member-search.service';
import { FabricAuthUserService } from '../../services/fabric-auth-user.service';
import { FabricExternalIdpSearchService } from '../../services/fabric-external-idp-search.service';
import { httpInterceptorProviders } from '../../services/interceptors';


import { MemberListComponent } from './member-list/member-list.component';
import { MemberAddComponent } from './member-add/member-add.component';


@NgModule({
  imports: [
    CommonModule,
    AccessControlRoutingModule
  ],
  declarations: [MemberListComponent, MemberAddComponent],  
  providers: [
    FabricAuthBaseService,
    FabricAuthGroupService,
    FabricAuthMemberSearchService,
    FabricAuthUserService,
    FabricExternalIdpSearchService   
  ],
  exports:[
  ]
})
export class AccessControlModule { }
