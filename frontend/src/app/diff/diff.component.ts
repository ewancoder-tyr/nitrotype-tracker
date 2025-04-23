import { ChangeDetectionStrategy, Component, HostBinding, Input } from '@angular/core';

@Component({
    selector: 'tnt-diff',
    imports: [],
    templateUrl: './diff.component.html',
    styleUrl: './diff.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class DiffComponent {
    @Input({ required: true }) value!: number;
    @Input() decimalDigits = 4;

    @HostBinding('class')
    protected get getClass() {
        if (this.value < 0) return 'bad';

        return 'good';
    }

    protected getValue() {
        if (this.value === 0) return '';

        const value = Math.abs(this.value).toFixed(this.decimalDigits);

        return this.value < 0 ? `\u25bc ${value}` : `\u25b2 ${value}`;
    }
}
