import { Component, OnInit } from '@angular/core';

import { FabricExternalIdpSearchService } from '../../../services/fabric-external-idp-search.service';

@Component({
  selector: 'app-member-add',
  templateUrl: './member-add.component.html',
  styleUrls: ['./member-add.component.css']
})
export class MemberAddComponent implements OnInit {

  searchInput: string;
  principals: any;
  constructor(private idpSearchService: FabricExternalIdpSearchService) { }

  ngOnInit() {
  }

  onKey(searchText){
    this.searchInput = searchText;


  }

  getRoles(){
    
  }
}
