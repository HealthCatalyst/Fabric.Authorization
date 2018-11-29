import { Directive, ElementRef, Renderer2, Input, OnChanges } from '@angular/core';

@Directive({
  selector: '[highlight]'
})
export class InputDirective implements OnChanges {

  @Input() highlight: boolean;

  constructor(private el: ElementRef, private renderer: Renderer2) { }

  ngOnChanges() {
    if (this.highlight === true) {
      this.renderer.setStyle(this.el.nativeElement, 'border', '1.5px solid red');
    }
  }

}
