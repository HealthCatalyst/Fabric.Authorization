import { Component, OnInit } from '@angular/core';
import { Observable } from 'rxjs/Rx';

import { FabricAuthMemberSearchService } from '../../../services/fabric-auth-member-search.service';
import { AuthMemberSearchRequest } from '../../../models/authMemberSearchRequest';
import { AuthMemberSearchResult } from '../../../models/authMemberSearchResult';

@Component({
  selector: 'app-member-list',
  templateUrl: './member-list.component.html',
  styleUrls: ['./member-list.component.css']
})
export class MemberListComponent implements OnInit {

  members: AuthMemberSearchResult[];

  constructor(private memberSearchService: FabricAuthMemberSearchService) { }

  ngOnInit() {
    this.getMembers();
  }

  getMembers() {
    var self = this;
    var searchRequest = new AuthMemberSearchRequest();
    searchRequest.clientId = 'fabric-angularsample';

    return this.memberSearchService.searchMembers(searchRequest)
    .subscribe(function(memberList){
      self.members = memberList;
    });
  }

}
