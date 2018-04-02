import { Component, OnInit } from '@angular/core';

import {
  FabricExternalIdpSearchService,
  FabricAuthRoleService,
  AccessControlConfigService,
  FabricAuthGroupService
} from '../../../services';
import {
  IFabricPrincipal,
  IRole,
  IUser,
  IGroup
} from '../../../models';

@Component({
  selector: 'app-custom-group-add',
  templateUrl: './custom-group-add.component.html',
  styleUrls: ['./custom-group-add.component.scss']
})
export class CustomGroupAddComponent implements OnInit {
  public customGroupName = '';
  public searchInput: string;
  public users: Array<IFabricPrincipal> = [];
  public selectedUsers: Array<IFabricPrincipal> = [];
  public roles: Array<IRole> = [];
  public selectedRoles: Array<IRole> = [];

  constructor(
    private idpSearchService: FabricExternalIdpSearchService,
    private roleService: FabricAuthRoleService,
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

    this.idpSearchService.searchExternalIdP(searchText, 'user').subscribe(result => {
      this.users = result.principals;
    });
  }

  onUserSelect(user: IFabricPrincipal) {
    if (!this.userIsSelected(user)) {
      this.selectedUsers.push(user);
    } else {
      this.selectedUsers = this.selectedUsers.filter(
        u => u.subjectId !== user.subjectId
      );
    }
  }

  userIsSelected(user: IFabricPrincipal) {
    return (
      this.selectedUsers.filter(u => u.subjectId === user.subjectId).length > 0
    );
  }

  removeUserSelection(user: IUser) {
    this.selectedUsers = this.selectedUsers.filter(
      u => u.subjectId !== user.subjectId
    );
  }

  onRoleSelect(role: IRole) {
    if (!this.roleIsSelected(role)) {
      this.selectedRoles.push(role);
    } else {
      this.selectedRoles = this.selectedRoles.filter(r => r.name !== role.name);
    }
  }

  roleIsSelected(role: IRole) {
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
    const group: IGroup = { groupName: this.customGroupName,  groupSource: 'custom'};
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
        const usersToSave = new Array<IUser>();
        this.selectedUsers.forEach(function(user) {
          const userModel: IUser = { identityProvider, subjectId: user.subjectId };
          usersToSave.push(userModel);
        });
        return this.groupService
          .addUsersToCustomGroup(group.groupName, usersToSave)
          .toPromise();
      });
  }
}
