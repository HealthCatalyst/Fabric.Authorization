import { Component, OnInit } from '@angular/core';
import { MatTreeFlatDataSource, MatTreeFlattener } from '@angular/material/tree';
import { FlatTreeControl } from '@angular/cdk/tree';
import { Observable } from 'rxjs';
import { of as observableOf } from 'rxjs/observable/of';

import { FabricAuthGrainService } from '../../../services/fabric-auth-grain.service';
import { IGrain } from '../../../models/grain.model';
import { ISecurableItem } from '../../../models/securableItem.model';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-grain-list',
  templateUrl: './grain-list.component.html',
  styleUrls: ['./grain-list.component.scss', '../access-control.scss']
})
export class GrainListComponent implements OnInit {

  treeControl: FlatTreeControl<GrainFlatNode>;
  treeFlattener: MatTreeFlattener<GrainNode, GrainFlatNode>;
  dataSource: MatTreeFlatDataSource<GrainNode, GrainFlatNode>;

  grains: IGrain[];
  isGrainVisible = false;
  selectedNode: GrainFlatNode;

  transformer = (node: GrainNode, level: number) => new GrainFlatNode(!!node.children, node.name, node.parentName, level);

  private _getLevel = (node: GrainFlatNode) => node.level;
  private _isExpandable = (node: GrainFlatNode) => node.expandable;
  private _getChildren = (node: GrainNode): Observable<GrainNode[]> => observableOf(node.children);
  hasChild = (_: number, _nodeData: GrainFlatNode) => _nodeData.expandable;

  constructor(
    private route: ActivatedRoute,
    private grainService: FabricAuthGrainService
  ) {
    this.isGrainVisible = grainService.isGrainVisible();
    this.treeFlattener = new MatTreeFlattener(this.transformer, this._getLevel, this._isExpandable, this._getChildren);
    this.treeControl = new FlatTreeControl<GrainFlatNode>(this._getLevel, this._isExpandable);
    this.dataSource = new MatTreeFlatDataSource(this.treeControl, this.treeFlattener);

    const routeGrain = this.route.snapshot.params['grain'];
    if (!!routeGrain) {
      this.setSelectedNode(routeGrain, this.route.snapshot.paramMap.get('securableItem'));
    }

    this.grainService.getAllGrains().subscribe(
      response => {
        const sortedGrains = response;

        if (sortedGrains.length > 1) {
          const dosIndex = response.findIndex(g => g.name === 'dos');
          if (dosIndex > 0) {
            sortedGrains.unshift(sortedGrains[dosIndex]);
            sortedGrains.splice(dosIndex + 1, 1);
          }
        }

        this.dataSource.data = this.addGrainToGrainNode(sortedGrains);
        this.initializeSelectedNode();
      });
  }

  ngOnInit() {
  }

  ifExists(obj: any): boolean {
    return !!obj;
  }

  initializeSelectedNode(): void {
    const selectedGrain = this.getSelectedGrain();
    const selectedSecurableItem = this.getSelectedSecurableItem();
    if (this.ifExists(selectedGrain)) {
      const grainNode = this.treeControl.dataNodes.find(node => {
        return node.name === selectedGrain;
      });

      this.treeControl.expand(grainNode);
      this.selectedNode = this.treeControl.dataNodes.find(node => {
        return node.name === selectedSecurableItem && node.parentName === selectedGrain;
      });

      // if selectedNode was not found (was null or did not exists), take first node under grain
      if (!this.ifExists(this.selectedNode)) {
        this.selectedNode = this.treeControl.dataNodes.find(node => {
          return node.parentName === selectedGrain;
        });
      }
     } else {
        const grainNode = this.treeControl.dataNodes[0];
        const secondNode = this.treeControl.dataNodes[1];
        this.treeControl.expand(grainNode);

        // if the second node a child, select that.
        // if it is not related, just use the first node.
        if (grainNode.name === secondNode.parentName) {
          this.selectedNode = secondNode;
          this.setSelectedNode(grainNode.name, secondNode.name);
        } else {
          this.selectedNode = grainNode;
          this.setSelectedNode(grainNode.name);
        }
      }
  }

  getSelectedGrain() {
    return sessionStorage.getItem('selectedGrain');
  }

  getSelectedSecurableItem() {
    return sessionStorage.getItem('selectedSecurableItem');
  }

  addGrainToGrainNode(data: IGrain[]): GrainNode[] {
    const result: GrainNode[] = [];

    for (const item of data) {
      const node = new GrainNode();
      node.name = item.name;
      node.children = this.addSecurableItemToGrainNode(item.securableItems, item.name);
      result.push(node);
    }

    return result;
  }

  addSecurableItemToGrainNode(data: ISecurableItem[], parentName: string): GrainNode[] {
    const result: GrainNode[] = [];

    if (data === null) {
      return null;
    }

    for (const item of data) {
      const node = new GrainNode();
      node.name = item.name;
      node.parentName = parentName;

      node.children = this.addSecurableItemToGrainNode(item.securableItems, item.name);
      result.push(node);
    }

    return result;
  }

  onSelect(node: GrainFlatNode): void {
    if (!!node.parentName) {
      this.selectedNode = node;
    }

    this.setSelectedNode(this.selectedNode.parentName, this.selectedNode.name);
  }

  setSelectedNode(grain: string, securableItem?: string) {
    const location = window.location;
    const url = `${location.protocol}//${location.host}/client/access-control`;
    if (!!securableItem) {
      sessionStorage.setItem('selectedGrain', grain);
      sessionStorage.setItem('selectedSecurableItem', securableItem);
      history.pushState({id: `${grain}_${securableItem}`}, null, `${url}/${grain}/${securableItem}`);
    } else {
      sessionStorage.removeItem('selectedSecurableItem');
      sessionStorage.setItem('selectedGrain', grain);
      history.pushState({id: `${grain}`}, null, `${url}/${grain}`);
    }
  }

  getIcon(node: GrainFlatNode): string {
    const isNodeExpanded = this.treeControl.isExpanded(node);
    const isGrain = !(!!node.parentName);
    const isSelectedNode = this.selectedNode === node;

    if (isGrain && isNodeExpanded) {
      return 'fa-angle-down';
    } else if (isGrain && !isNodeExpanded) {
      return 'fa-angle-right';
    }

    if (!isGrain && isSelectedNode) {
      return 'fa-angle-right';
    } else {
      return 'fa-angle-down';
    }
  }

  isSelectedNode(myNode: GrainFlatNode): string {
    return this.selectedNode === myNode ? 'selected' : '';
  }
}

export class GrainNode {
  children: GrainNode[];
  name: string;
  parentName: string;
}

export class GrainFlatNode {
  constructor(
    public expandable: boolean, public name: string, public parentName: string, public level: number) { }
}
