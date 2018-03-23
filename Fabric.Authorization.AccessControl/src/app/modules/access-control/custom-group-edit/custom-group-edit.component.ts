import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { Role, 
         User,
         FabricPrincipal,
         IdPSearchRequest } from '../../../models';
import { FabricAuthUserService,
          AccessControlConfigService,
          FabricAuthRoleService,
          FabricAuthGroupService,
          FabricExternalIdpSearchService } from '../../../services';

@Component({
  selector: 'app-custom-group-edit',
  templateUrl: './custom-group-edit.component.html',
  styleUrls: ['./custom-group-edit.component.css']
})
export class CustomGroupEditComponent implements OnInit {

  public groupName: string = '';
  public roles: Array<Role> = [];
  public assignableRoles: Array<Role> = [];
  public rolesToAssign: Array<Role> = [];
  public usersToAssign: Array<FabricPrincipal> = [];
  public selectedUsers: Array<FabricPrincipal> = [];
  public users: Array<User> = [];

  constructor(private route: ActivatedRoute,
              private userService: FabricAuthUserService,
              private configService: AccessControlConfigService,
              private roleService: FabricAuthRoleService,
              private groupService: FabricAuthGroupService,
              private idpSearchService: FabricExternalIdpSearchService) { }

  ngOnInit() {
    this.groupName = this.route.snapshot.paramMap.get('subjectid');
    this.getGroupRoles();
    this.getGroupUsers();
  }

  onKey(searchText){
    if(searchText.length === 0){
      this.usersToAssign = [];
    }
    if(searchText.length < 2) {
      return;
    }
    var request = new IdPSearchRequest(searchText);      
    request.type = 'user';

    this.idpSearchService.searchExternalIdP(request)
    .subscribe(result => {
      this.usersToAssign = result.principals;
    });
  }

  onUserSelect(user: FabricPrincipal){
    if(!this.userIsSelected(user)){
      this.selectedUsers.push(user);
    }else{
      this.selectedUsers = this.selectedUsers.filter(u => u.subjectId !== user.subjectId);
    }
  }

  userIsSelected(user: FabricPrincipal){
    return this.selectedUsers.filter(u => u.subjectId === user.subjectId).length > 0;
  }

  removeUserSelection(user: User){
    this.selectedUsers = this.selectedUsers.filter(u => u.subjectId !== user.subjectId);
  }

  getGroupUsers(){
    return this.groupService.getGroupUsers(this.groupName)
    .toPromise()
    .then(users => {
      this.users = users;
    })
  }

  removeUserFromGroup(user: User){
    return this.groupService.removeUserFromCustomGroup(this.groupName, user)
    .toPromise()
    .then(() => this.getGroupUsers());
  }

  getGroupRoles(){   
    return this.groupService.getGroupRoles(this.groupName, this.configService.grain, this.configService.securableItem)
      .toPromise()
      .then((roles) => {
        this.roles = roles;
        return this.getRolesToAssign();
      });
  }

  removeRole(role: Role){    
    this.roles = this.removeMemberRole(role);
    let rolesToRemove: Array<Role> = [];
    rolesToRemove.push(role);

    return this.groupService.removeRolesFromGroup(this.groupName, rolesToRemove)
    .toPromise()
    .then(() => this.getRolesToAssign());
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
    return this.groupService.addRolesToGroup(this.groupName, this.rolesToAssign)
    .toPromise()
    .then(() => this.getGroupRoles());
  }

  updateGroup(){
    //add users from selectedUsers
    if(this.selectedUsers.length > 0){
      var identityProvider = this.configService.identityProvider;
      var usersToSave = new Array<User>();
      this.selectedUsers.forEach(function(user){
        var userModel = new User(identityProvider, user.subjectId);
        usersToSave.push(userModel);
      });
      this.groupService.addUsersToCustomGroup(this.groupName, usersToSave).toPromise();
    }
    //add roles from rolesToAssign
    if(this.rolesToAssign.length > 0){
      this.groupService.addRolesToGroup(this.groupName, this.rolesToAssign).toPromise();      
    }
  }

  private removeMemberRole(role: Role){
    return this.roles.filter(r => r.name !== role.name);
  }  
}
