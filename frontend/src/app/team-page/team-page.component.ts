import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RacersTableComponent } from '../racers-table/racers-table.component';

@Component({
    selector: 'tnt-team-page',
    imports: [RacersTableComponent],
    templateUrl: './team-page.component.html',
    styleUrl: './team-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class TeamPageComponent {}
