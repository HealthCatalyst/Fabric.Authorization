import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute, ParamMap } from '@angular/router';
import 'rxjs/add/operator/switchMap';

import { Role, 
         User } from '../../../models';
import { FabricAuthUserService,
        AccessControlConfigService,
        FabricAuthRoleService } from '../../../services';
    

@Component({
  selector: 'app-member-edit',
  templateUrl: './member-edit.component.html',
  styleUrls: ['./member-edit.component.css']
})
export class MemberEditComponent implements OnInit {

  public roles: Array<Role> = [];
  public assignableRoles: Array<Role> = [];
  public subjectId: string = '';
  public identityProvider: string = '';
  public rolesToAssign: Array<Role> = [];

  constructor( private route: ActivatedRoute,
              private router: Router,
              private userService: FabricAuthUserService,
              private configService: AccessControlConfigService,
              private roleService: FabricAuthRoleService) { }

  ngOnInit() {
    this.subjectId = this.route.snapshot.paramMap.get('subjectid');
    this.identityProvider = this.route.snapshot.paramMap.get('idprovider');

    return this.getUserRoles();
  }

  getUserRoles(){
    return this.userService.getUserRoles(this.identityProvider, this.subjectId)
    .toPromise()
    .then((roles) => {     
      this.roles = roles;
      return this.getRolesToAssign();
    });
  }

  removeRole(role: Role){
    var self = this;
    this.roles = this.removeMemberRole(role);
    let rolesToRemove: Array<Role> = [];
    rolesToRemove.push(role);
    return this.userService.removeRolesFromUser(this.identityProvider, this.subjectId, rolesToRemove)
    .toPromise()
    .then(self.getRolesToAssign);
  }

  getRolesToAssign(){
    return this.roleService.getRolesBySecurableItemAndGrain(this.configService.grain, this.configService.securableItem)
    .toPromise()
    .then((roles) =>{
      let existingRoleNames = this.roles.map(r => r.name);
      this.assignableRoles = roles.filter(r => existingRoleNames.indexOf(r.name) === -1);
    });
  }

  onRoleSelect(role: Role){
    if(!this.roleIsSelected(role)){
      this.rolesToAssign.push(role);
    } else{
      this.rolesToAssign = this.rolesToAssign.filter(r => r.name !== role.name);
    }
  }

  roleIsSelected(role: Role){
    return this.rolesToAssign.filter(r => r.name === role.name).length > 0;
  }

  addRoles(){
    var self = this;
    return this.userService.addRolesToUser(this.identityProvider, this.subjectId, this.rolesToAssign)
    .toPromise()
    .then(self.getUserRoles);
  }

  private removeMemberRole(role: Role){
    return this.roles.filter(r => r.name !== role.name);
  }
}
