import { ChangeDetectionStrategy, Component, HostBinding, Input, OnInit } from '@angular/core';
import { RacerInfo } from '../racers-table/racers-table.component';

@Component({
    selector: 'tnt-racer-name',
    imports: [],
    templateUrl: './racer-details.component.html',
    styleUrl: './racer-details.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class RacerDetailsComponent {
    @Input({ required: true }) racer!: RacerInfo;
    @Input({ required: true }) detail!: RacerDetail;

    @HostBinding('class')
    get hostClass(): string {
        if (this.detail !== RacerDetail.Name && this.detail !== RacerDetail.RacesPlayed) return '';

        if (this.racer.racesPlayed >= 1000) return 'the best';
        if (this.racer.racesPlayed >= 500) return 'awesome';
        if (this.racer.racesPlayed >= 250) return 'epic';
        if (this.racer.racesPlayed >= 100) return 'unique';
        if (this.racer.racesPlayed >= 50) return 'rare';
        if (this.racer.racesPlayed >= 20) return 'magic';

        return 'getting there';
    }

    @HostBinding('title')
    get hostTitle(): string {
        return `You are ${this.hostClass}!`;
    }

    getDetail() {
        if (this.detail === RacerDetail.Name) return this.racer.name;
        if (this.detail === RacerDetail.RacesPlayed) return this.racer.racesPlayed;

        return '';
    }
}

export enum RacerDetail {
    Name = 0,
    RacesPlayed
}
