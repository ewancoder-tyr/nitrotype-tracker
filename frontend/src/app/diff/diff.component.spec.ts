import { ComponentFixture, TestBed } from '@angular/core/testing';

import { provideExperimentalZonelessChangeDetection } from '@angular/core';
//import { tryInit } from '../test-init';
import { DiffComponent } from './diff.component';

describe('DiffComponent', () => {
    let component: DiffComponent;
    let fixture: ComponentFixture<DiffComponent>;

    beforeEach(async () => {
        //tryInit();
        await TestBed.configureTestingModule({
            providers: [provideExperimentalZonelessChangeDetection()],
            imports: [DiffComponent]
        }).compileComponents();

        fixture = TestBed.createComponent(DiffComponent);
        component = fixture.componentInstance;
    });

    describe('when component is initialized', () => {
        it('should initialize', () => expect(component).toBeDefined());
    });

    describe('when value is negative', () => {
        beforeEach(async () => {
            component.value = -10;
            await fixture.whenStable();
        });

        it('should have bad class', () => {
            expect(fixture.nativeElement.classList.contains('bad')).toBeTrue();
        });
    });

    describe('when value is positive', () => {
        beforeEach(async () => {
            component.value = 10;
            await fixture.whenStable();
        });

        it('should have good class', () => {
            expect(fixture.nativeElement.classList.contains('good')).toBeTrue();
        });
    });
});
