import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { config } from '../config';

@Component({
    selector: 'tnt-root',
    imports: [RouterOutlet],
    templateUrl: './app.component.html',
    styleUrl: './app.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class AppComponent {
    ngOnInit() {
        console.log(`Configuration environment: ${config.environment}`);
    }
}
