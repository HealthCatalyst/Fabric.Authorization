import { Component, OnInit } from '@angular/core';
import { Observable } from 'rxjs/Rx';

import { AccessControlConfigService } from '../../../services';

import { FabricAuthMemberSearchService } from '../../../services/fabric-auth-member-search.service';
import { AuthMemberSearchRequest, AuthMemberSearchResult } from '../../../models'

@Component({
  selector: 'app-member-list',
  templateUrl: './member-list.component.html',
  styleUrls: ['./member-list.component.css']
})
export class MemberListComponent implements OnInit {

  members: AuthMemberSearchResult[];
  filterText: string;

  constructor(private memberSearchService: FabricAuthMemberSearchService, private configService: AccessControlConfigService) { }

  ngOnInit() {
    this.getMembers();
  }

  getMembers() {
    var self = this;
    var searchRequest = new AuthMemberSearchRequest();
    searchRequest.clientId = this.configService.clientId;
    searchRequest.filter = this.filterText;
    

    return this.memberSearchService.searchMembers(searchRequest)
    .subscribe(function(memberList){
      self.members = memberList;
    });
  }

}
