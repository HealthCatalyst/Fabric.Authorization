import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { IRole, IUser, IFabricPrincipal } from '../../../models';
import {
  FabricAuthUserService,
  AccessControlConfigService,
  FabricAuthRoleService,
  FabricAuthGroupService,
  FabricExternalIdpSearchService
} from '../../../services';

@Component({
  selector: 'app-custom-group-edit',
  templateUrl: './custom-group-edit.component.html',
  styleUrls: ['./custom-group-edit.component.scss']
})
export class CustomGroupEditComponent implements OnInit {
  public groupName = '';
  public roles: Array<IRole> = [];
  public assignableRoles: Array<IRole> = [];
  public rolesToAssign: Array<IRole> = [];
  public usersToAssign: Array<IFabricPrincipal> = [];
  public selectedUsers: Array<IFabricPrincipal> = [];
  public users: Array<IUser> = [];

  constructor(
    private route: ActivatedRoute,
    private userService: FabricAuthUserService,
    private configService: AccessControlConfigService,
    private roleService: FabricAuthRoleService,
    private groupService: FabricAuthGroupService,
    private idpSearchService: FabricExternalIdpSearchService
  ) {}

  ngOnInit() {
    this.groupName = this.route.snapshot.paramMap.get('subjectid');
    this.getGroupRoles();
    this.getGroupUsers();
  }

  onKey(searchText) {
    if (searchText.length === 0) {
      this.usersToAssign = [];
    }
    if (searchText.length < 2) {
      return;
    }

    this.idpSearchService.searchExternalIdP(searchText, 'user').subscribe(result => {
      this.usersToAssign = result.principals;
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

  getGroupUsers() {
    return this.groupService
      .getGroupUsers(this.groupName)
      .toPromise()
      .then(users => {
        this.users = users;
      });
  }

  removeUserFromGroup(user: IUser) {
    return this.groupService
      .removeUserFromCustomGroup(this.groupName, user)
      .toPromise()
      .then(() => this.getGroupUsers());
  }

  getGroupRoles() {
    return this.groupService
      .getGroupRoles(
        this.groupName,
        this.configService.grain,
        this.configService.securableItem
      )
      .toPromise()
      .then(roles => {
        this.roles = roles;
        return this.getRolesToAssign();
      });
  }

  removeRole(role: IRole) {
    this.roles = this.removeMemberRole(role);
    const rolesToRemove: Array<IRole> = [];
    rolesToRemove.push(role);

    return this.groupService
      .removeRolesFromGroup(this.groupName, rolesToRemove)
      .toPromise()
      .then(() => this.getRolesToAssign());
  }

  getRolesToAssign() {
    return this.roleService
      .getRolesBySecurableItemAndGrain(
        this.configService.grain,
        this.configService.securableItem
      )
      .toPromise()
      .then(roles => {
        const existingRoleNames = this.roles.map(r => r.name);
        this.assignableRoles = roles.filter(
          r => existingRoleNames.indexOf(r.name) === -1
        );
      });
  }

  onRoleSelect(role: IRole) {
    if (!this.roleIsSelected(role)) {
      this.rolesToAssign.push(role);
    } else {
      this.rolesToAssign = this.rolesToAssign.filter(r => r.name !== role.name);
    }
  }

  roleIsSelected(role: IRole) {
    return this.rolesToAssign.filter(r => r.name === role.name).length > 0;
  }

  addRoles() {
    return this.groupService
      .addRolesToGroup(this.groupName, this.rolesToAssign)
      .toPromise()
      .then(() => this.getGroupRoles());
  }

  updateGroup() {
    // add users from selectedUsers
    if (this.selectedUsers.length > 0) {
      const identityProvider = this.configService.identityProvider;
      const usersToSave = new Array<IUser>();
      this.selectedUsers.forEach(function(user) {
        const userModel: IUser = {identityProvider, subjectId: user.subjectId};
        usersToSave.push(userModel);
      });
      this.groupService
        .addUsersToCustomGroup(this.groupName, usersToSave)
        .toPromise();
    }
    // add roles from rolesToAssign
    if (this.rolesToAssign.length > 0) {
      this.groupService
        .addRolesToGroup(this.groupName, this.rolesToAssign)
        .toPromise();
    }
  }

  private removeMemberRole(role: IRole) {
    return this.roles.filter(r => r.name !== role.name);
  }
}
