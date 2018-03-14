import { Component, OnInit } from '@angular/core';

import {  FabricExternalIdpSearchService, 
  FabricAuthRoleService, 
  AccessControlConfigService,
  FabricAuthUserService,
  FabricAuthGroupService } from '../../../services';
import {  FabricPrincipal, 
  Role, 
  User,
  Group,
  IdPSearchRequest } from '../../../models';


@Component({
  selector: 'app-custom-group-add',
  templateUrl: './custom-group-add.component.html',
  styleUrls: ['./custom-group-add.component.css']
})
export class CustomGroupAddComponent implements OnInit {

  public customGroupName: string = "";
  public searchInput: string;
  public users: Array<FabricPrincipal> = [];
  public selectedUsers: Array<FabricPrincipal> = [];
  public roles: Array<Role> = [];
  public selectedRoles: Array<Role> = [];  

  constructor(private idpSearchService: FabricExternalIdpSearchService, 
    private roleService: FabricAuthRoleService,
    private userService: FabricAuthUserService,
    private configService: AccessControlConfigService,
    private groupService: FabricAuthGroupService) { }

    ngOnInit() {
      this.getRoles();
    }
  
    onKey(searchText){
      if(searchText.length < 2) {
        return;
      }
      var request = new IdPSearchRequest(searchText);      
      request.type = 'users';
  
      this.idpSearchService.searchExternalIdP(request)
      .subscribe(result => {
        this.users = result.principals;
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
  
    onRoleSelect(role: Role){
      if(!this.roleIsSelected(role)){
        this.selectedRoles.push(role);
      } else{
        this.selectedRoles = this.selectedRoles.filter(r => r.name !== role.name);
      }
    }
  
    roleIsSelected(role: Role){
      return this.selectedRoles.filter(r => r.name === role.name).length > 0;
    }
  
    getRoles(){
      this.roleService.getRolesBySecurableItemAndGrain(this.configService.grain, this.configService.securableItem)
        .subscribe(roleResults => {
          this.roles = roleResults;
        });
    }
  
    addGroupWithUsersAndRoles(){      
        var group = new Group(this.customGroupName, 'custom');
        this.groupService.createGroup(group).toPromise()
          .then(newGroup => {
            return this.groupService.addRolesToGroup(newGroup.groupName, this.selectedRoles).toPromise();
          });
        
        //var user = new User(this.configService.identityProvider, this.selectedPrincipal.subjectId);      
        // return this.userService.createUser(user).toPromise()
        // .then((user: User) => {

        // });                  
      }    

}
