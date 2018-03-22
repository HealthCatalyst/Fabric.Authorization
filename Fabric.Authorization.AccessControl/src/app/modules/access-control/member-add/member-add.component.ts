import {  Component, OnInit } from '@angular/core';
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
  selector: 'app-member-add',
  templateUrl: './member-add.component.html',
  styleUrls: ['./member-add.component.css']
})
export class MemberAddComponent implements OnInit {

  public searchInput: string;
  public principals: Array<FabricPrincipal>;
  public selectedPrincipal: FabricPrincipal;
  public roles: Array<Role>;
  public selectedRoles: Array<Role>;  
  

  constructor(private idpSearchService: FabricExternalIdpSearchService, 
    private roleService: FabricAuthRoleService,
    private userService: FabricAuthUserService,
    private configService: AccessControlConfigService,
    private groupService: FabricAuthGroupService
    ) {
      this.selectedRoles = [];
     }

  ngOnInit() {
    this.getRoles();
  }

  onKey(searchText){
    if(searchText.length < 2) {
      return;
    }
    var request = new IdPSearchRequest(searchText);

    this.idpSearchService.searchExternalIdP(request)
    .subscribe(result => {
      this.principals = result.principals;
    });
  }

  onPrincipalSelect(principal: FabricPrincipal){
    this.selectedPrincipal = principal;
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

  addMemberWithRoles(){
    if(this.selectedPrincipal.principalType == 'user'){
      
      var user = new User(this.configService.identityProvider, this.selectedPrincipal.subjectId);      
      return this.userService.getUser(this.configService.identityProvider, this.selectedPrincipal.subjectId)
      .toPromise()
      .then((userResult: User) => {        
          return userResult;                
      })
      .catch(err => {
        if(err.statusCode === 404){
          return this.userService.createUser(user).toPromise();
        }
      })      
      .then((user: User) => {
        return this.userService.addRolesToUser(user.identityProvider, user.subjectId, this.selectedRoles).toPromise();
      });            
    }else {
      var group = new Group(this.selectedPrincipal.subjectId, '');
      return this.groupService.getGroup(group.groupName)
      .toPromise()
      .then((groupResult: Group) =>{
        return groupResult;
      })
      .catch(err => {
        if(err.statusCode == 404){
          return  this.groupService.createGroup(group).toPromise()
        }
      })
      .then((newGroup: Group) => {
          this.groupService.addRolesToGroup(newGroup.groupName, this.selectedRoles).toPromise();
        });
    }   
  }  
}
