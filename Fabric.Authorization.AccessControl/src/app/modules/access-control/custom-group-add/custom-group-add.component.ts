import { Component, OnInit } from '@angular/core';

import {
  FabricExternalIdpSearchService,
  FabricAuthRoleService,
  AccessControlConfigService,
  FabricAuthUserService,
  FabricAuthGroupService
} from '../../../services';
import {
  FabricPrincipal,
  Role,
  User,
  Group,
  IdPSearchRequest
} from '../../../models';

@Component({
  selector: 'app-custom-group-add',
  templateUrl: './custom-group-add.component.html',
  styleUrls: ['./custom-group-add.component.css']
})
export class CustomGroupAddComponent implements OnInit {
  public customGroupName = '';
  public searchInput: string;
  public users: Array<FabricPrincipal> = [];
  public selectedUsers: Array<FabricPrincipal> = [];
  public roles: Array<Role> = [];
  public selectedRoles: Array<Role> = [];

  constructor(
    private idpSearchService: FabricExternalIdpSearchService,
    private roleService: FabricAuthRoleService,
    private userService: FabricAuthUserService,
    private configService: AccessControlConfigService,
    private groupService: FabricAuthGroupService
  ) {}

  ngOnInit() {
    this.getRoles();
  }

  onKey(searchText) {
    if (searchText.length === 0) {
      this.users = [];
    }
    if (searchText.length < 2) {
      return;
    }
    const request = new IdPSearchRequest(searchText);
    request.type = 'user';

    this.idpSearchService.searchExternalIdP(request).subscribe(result => {
      this.users = result.principals;
    });
  }

  onUserSelect(user: FabricPrincipal) {
    if (!this.userIsSelected(user)) {
      this.selectedUsers.push(user);
    } else {
      this.selectedUsers = this.selectedUsers.filter(
        u => u.subjectId !== user.subjectId
      );
    }
  }

  userIsSelected(user: FabricPrincipal) {
    return (
      this.selectedUsers.filter(u => u.subjectId === user.subjectId).length > 0
    );
  }

  removeUserSelection(user: User) {
    this.selectedUsers = this.selectedUsers.filter(
      u => u.subjectId !== user.subjectId
    );
  }

  onRoleSelect(role: Role) {
    if (!this.roleIsSelected(role)) {
      this.selectedRoles.push(role);
    } else {
      this.selectedRoles = this.selectedRoles.filter(r => r.name !== role.name);
    }
  }

  roleIsSelected(role: Role) {
    return this.selectedRoles.filter(r => r.name === role.name).length > 0;
  }

  getRoles() {
    this.roleService
      .getRolesBySecurableItemAndGrain(
        this.configService.grain,
        this.configService.securableItem
      )
      .subscribe(roleResults => {
        this.roles = roleResults;
      });
  }

  addGroupWithUsersAndRoles() {
    const group = new Group(this.customGroupName, 'custom');
    this.groupService
      .createGroup(group)
      .toPromise()
      .then(newGroup => {
        return this.groupService
          .addRolesToGroup(newGroup.groupName, this.selectedRoles)
          .toPromise();
      })
      .then(() => {
        const identityProvider = this.configService.identityProvider;
        const usersToSave = new Array<User>();
        this.selectedUsers.forEach(function(user) {
          const userModel = new User(identityProvider, user.subjectId);
          usersToSave.push(userModel);
        });
        return this.groupService
          .addUsersToCustomGroup(group.groupName, usersToSave)
          .toPromise();
      });
  }
}
