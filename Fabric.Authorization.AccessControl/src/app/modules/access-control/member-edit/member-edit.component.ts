import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute, ParamMap } from '@angular/router';
import 'rxjs/add/operator/switchMap';

import { Role, 
         User } from '../../../models';
import { FabricAuthUserService,
        AccessControlConfigService,
        FabricAuthRoleService,
        FabricAuthGroupService } from '../../../services';
    

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
  public entityType: string; 

  constructor( private route: ActivatedRoute,
              private router: Router,
              private userService: FabricAuthUserService,
              private configService: AccessControlConfigService,
              private roleService: FabricAuthRoleService,
              private groupService: FabricAuthGroupService) { }

  ngOnInit() {
    this.subjectId = this.route.snapshot.paramMap.get('subjectid');
    this.entityType = this.route.snapshot.paramMap.get('type').toLowerCase();
    this.identityProvider = this.configService.identityProvider;

    return this.getMemberRoles();
  }

  getMemberRoles(){
    if(this.entityType === 'user'){
      return this.userService.getUserRoles(this.identityProvider, this.subjectId)
      .toPromise()
      .then((roles) => {     
        this.roles = roles;
        return this.getRolesToAssign();
      });
    }else{
      return this.groupService.getGroupRoles(this.subjectId, this.configService.grain, this.configService.securableItem)
        .toPromise()
        .then((roles) => {
          this.roles = roles;
          return this.getRolesToAssign();
        })
    }    
  }

  removeRole(role: Role){    
    this.roles = this.removeMemberRole(role);
    let rolesToRemove: Array<Role> = [];
    rolesToRemove.push(role);

    if(this.entityType == 'user'){
      return this.userService.removeRolesFromUser(this.identityProvider, this.subjectId, rolesToRemove)
      .toPromise()
      .then(() => this.getRolesToAssign());
    }else{
      return this.groupService.removeRolesFromGroup(this.subjectId, rolesToRemove)
      .toPromise()
      .then(() => this.getRolesToAssign());
    }
    
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
    if(this.entityType === 'user'){
      return this.userService.addRolesToUser(this.identityProvider, this.subjectId, this.rolesToAssign)
    .toPromise()
    .then(() => this.getMemberRoles());
    }else{
      return this.groupService.addRolesToGroup(this.subjectId, this.rolesToAssign)
      .toPromise()
      .then(() => this.getMemberRoles())
    }
    
  }

  private removeMemberRole(role: Role){
    return this.roles.filter(r => r.name !== role.name);
  }
}
