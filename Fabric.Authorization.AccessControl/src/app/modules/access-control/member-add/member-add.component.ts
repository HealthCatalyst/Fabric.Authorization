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

  private grain: string = 'app';
  public searchInput: string;
  public principals: Array<FabricPrincipal>;
  public selectedPrincipal: FabricPrincipal;
  public roles: Array<Role>;
  public selectedRoles: Array<Role>;
  public selectedSubjectId: string;

  constructor(private idpSearchService: FabricExternalIdpSearchService, 
    private roleService: FabricAuthRoleService,
    private userService: FabricAuthUserService,
    private configService: AccessControlConfigService,
    private groupService: FabricAuthGroupService
    ) {
      this.selectedPrincipal = null;
      this.selectedRoles = null;
      this.selectedSubjectId = '';
     }

  ngOnInit() {
    this.getRoles();
  }

  onKey(searchText){
    if(searchText.length < 2) {
      return;
    }
    var request = new IdPSearchRequest();
    request.searchText = searchText;

    this.idpSearchService.searchExternalIdP(request)
    .subscribe(result => {
      this.principals = result.principals;
    });
  }

  getRoles(){
    this.roleService.getRolesBySecurableItemAndGrain(this.grain, this.configService.clientId)
      .subscribe(roleResults => {
        this.roles = roleResults;
      });
  }

  addMemberWithRoles(){
    if(this.selectedPrincipal.principalType == 'user'){
      var user = new User(this.configService.identityProvider, this.selectedPrincipal.subjectId);
      this.userService.createUser(user)
      .subscribe(user => {
        this.userService.addRolesToUser(this.configService.identityProvider, this.selectedPrincipal.subjectId, this.selectedRoles);
      });
    }else {
      var group = new Group(this.selectedPrincipal.name, '');
      this.groupService.createGroup(group)
        .subscribe(newGroup => {
          this.groupService.addRolesToGroup(newGroup.groupName, this.selectedRoles);
        });
    }   
  }
}
