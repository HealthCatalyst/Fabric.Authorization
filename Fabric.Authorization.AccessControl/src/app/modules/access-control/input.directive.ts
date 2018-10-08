import { Directive, ElementRef, Renderer2, Input } from '@angular/core';

@Directive({
  selector: '[highlight]'
})
export class InputDirective {

  @Input() highlight: boolean;

  constructor(el: ElementRef, private renderer: Renderer2) {
    if (this.highlight) {
      this.renderer.setStyle(el.nativeElement, 'color', 'red');
      }
  }
}
