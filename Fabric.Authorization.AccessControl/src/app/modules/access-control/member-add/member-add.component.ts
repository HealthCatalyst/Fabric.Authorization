import {  Component, OnInit } from '@angular/core';
import {  FabricExternalIdpSearchService, 
          FabricAuthRoleService, 
          AccessControlConfigService,
          FabricAuthUserService,
          FabricAuthGroupService } from '../../../services';
import {  FabricPrincipal, 
          Role, 
          User,
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

  constructor(private idpSearchService: FabricExternalIdpSearchService, 
    private roleService: FabricAuthRoleService,
    private userService: FabricAuthUserService,
    private configService: AccessControlConfigService,
    private groupService: FabricAuthGroupService
    ) { }

  ngOnInit() {
  }

  onKey(searchText){
    this.searchInput = searchText;

    var request = new IdPSearchRequest();
    request.searchText = searchText;

    this.idpSearchService.searchExternalIdP(request)
    .subscribe(result => {
      this.principals = result.principals;
    });
  }

  getRoles(){
    //need to get roles by securable item and grain (how will we know those??)
    this.roleService.getRolesBySecurableItemAndGrain(this.grain, this.configService.clientId)
      .subscribe(roleResults => {
        this.roles = roleResults;
      });
  }

  addMemberWithRoles(){
    var user = new User(this.configService.identityProvider, this.selectedPrincipal.subjectId);

    if(this.selectedPrincipal.principalType == 'user'){
      this.userService.createUser(user)
      .subscribe(user => {
        //add the roles to the new member
        this.userService.addRolesToUser(this.configService.identityProvider, this.selectedPrincipal.subjectId, this.selectedRoles);
      });
    }else {
      
    }   
  }
}
