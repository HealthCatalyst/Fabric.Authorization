import { NgModule, ModuleWithProviders } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UserlistComponent } from './userlist/userlist.component';
import { UseraddComponent } from './useradd/useradd.component';
import { AuthserviceService } from '../../services/authservice.service';

@NgModule({
  imports: [
    CommonModule
  ],
  declarations: [UserlistComponent, UseraddComponent],  
  exports:[
    UserlistComponent,
    UseraddComponent
  ]
})
export class AccessControlModule { 

  static forRoot(): ModuleWithProviders{
    return{
      ngModule: AccessControlModule,
      providers: [AuthserviceService]      
    }
  }
}
