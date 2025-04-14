import {
    ChangeDetectionStrategy,
    Component,
    computed,
    OnInit,
    Signal,
    signal,
    WritableSignal
} from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { RacerDetail, RacerDetailsComponent } from '../racer-details/racer-details.component';

@Component({
    selector: 'tnt-racers-table',
    imports: [DecimalPipe, RacerDetailsComponent],
    templateUrl: './racers-table.component.html',
    styleUrl: './racers-table.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class RacersTableComponent implements OnInit {
    data: WritableSignal<RacerInfo[]> = signal([]);
    sorted: Signal<RacerInfo[]> = computed(() => {
        const data = this.data();
        return data
            .filter(x => x.username !== 'keyboard_elite_cat')
            .sort((a, b) => b.accuracy - a.accuracy);
    });

    ngOnInit() {
        fetch('https://api.tnt.typingrealm.com/api/statistics/kecats').then(data => {
            data.json().then(data => {
                this.data.set(data);
                console.log(data);
            });
        });
    }

    protected readonly RacerDetail = RacerDetail;
}

export interface RacerInfo {
    username: string;
    team: string;
    typed: number;
    errors: number;
    name: string;
    racesPlayed: number;
    timestamp: string;
    secs: number;
    accuracy: number;
    averageTextLength: number;
    averageSpeed: number;
    accuracyDiff: number;
    averageSpeedDiff: number;
    racesPlayedDiff: number;
    timeSpent: string;
}
